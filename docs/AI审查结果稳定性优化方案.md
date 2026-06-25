# AI 审查结果稳定性优化方案

## 问题描述

使用 DeepSeek 进行数据库命名规范检查时，**相同的输入规则、相同的数据库结构**，每次调用 AI 返回的结果都不一致。表现为：

- 同一字段有时报错、有时不报错
- 建议名称前后不一致
- 合规率 / 问题数量每次不同
- 输出表格格式偶尔偏移

## 原因分析

当前调用 DeepSeek API 时未指定 `temperature` 和 `seed` 参数，API 使用默认值：

| 参数 | 默认值 | 效果 |
|------|--------|------|
| `temperature` | 1.0 | 高随机性，每次输出不同 |
| `seed` | 无（随机） | 每次推理路径不同 |

对于**命名规范检查**这种确定性任务，结果波动是不可接受的。

---

## 统一架构分析（跨供应商 + 跨数据库）

### 各供应商当前状态

| 供应商 | API 格式 | 已设 `temperature` | 支持 `seed` | 当前输出 |
|--------|----------|-------------------|-------------|---------|
| 模拟 AI | 本地代码直接生成 Markdown | — | — | 硬编码 Markdown |
| 通义千问 | DashScope 格式（`input.prompt` + `parameters`） | 0.01 ✅ | ❌ 不支持 | 自由文本 Markdown |
| DeepSeek | OpenAI 兼容格式（`messages`） | ❌ 默认 1.0 | ❌ 未传 | 自由文本 Markdown |

### 核心设计原则：分层抽象

不应在每个供应商里各自做 JSON 解析，而是**统一在 `DatabaseService` 层处理**：

```
┌──────────────────────────────────────────────────────────┐
│  GenerateAIPrompt(schema, rules, dbType)                 │
│  生成统一的 Prompt（包含 JSON Schema 约束）              │
│  只根据 dbType 替换 "MySQL"/"PostgreSQL" 文字描述        │
└─────────────────────┬────────────────────────────────────┘
                      ▼
┌──────────────────────────────────────────────────────────┐
│  AIService / SimulateAIResponse                          │
│  各供应商按自己的 API 格式调用，但输出格式一致为 JSON     │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────┐       │
│  │ DeepSeek │  │ 通义千问 │  │ 模拟AI(本地)     │       │
│  │temperature│  │temperature│  │直接生成JSON     │       │
│  │=0 + seed │  │=0.01     │  │                  │       │
│  └────┬─────┘  └────┬─────┘  └───────┬──────────┘       │
└─────────┼────────────┼────────────────┼──────────────────┘
          ▼            ▼                ▼
┌──────────────────────────────────────────────────────────┐
│  ParseAndRenderReview(aiResponse)                        │
│  统一解析 JSON → ReviewResult → Markdown                 │
│  JSON 解析失败 → 降级为原始文本显示                       │
└──────────────────────────────────────────────────────────┘
```

### 统一 JSON Schema（跨供应商、跨数据库）

```json
{
  "summary": {
    "total_tables": 40,
    "total_columns": 594,
    "issues_count": 63,
    "compliance_rate": 89.39
  },
  "issues": [
    {
      "type": "字段名",
      "object": "data_permission_policy.identity_dim1_relation",
      "current_name": "identity_dim1_relation",
      "problem": "字段名中数字未与单词用下划线分隔",
      "suggestion": "identity_dim_1_relation"
    }
  ]
}
```

**这套 Schema 对 MySQL 和 PostgreSQL 完全通用**，只在 Prompt 的数据库类型描述上区分。

### 各供应商具体处理策略

| 供应商 | API 调用改动 | 输出约束手段 |
|--------|-------------|-------------|
| **DeepSeek** | 请求体中加 `temperature: 0` + `seed: 20240601` | API 参数 + Prompt 约束，双保险 |
| **通义千问** | 已有 `temperature: 0.01`，不支持 `seed`；参数中加 `result_format: "json"`（DashScope 支持） | 主要靠 Prompt 约束 + 较低的 temperature |
| **模拟 AI** | 将 `SimulateAIResponse` 输出从 Markdown 改为 JSON | 本地代码直接生成结构化数据 |

> **通义千问**的 DashScope API 格式与 OpenAI 不同，它使用 `input.prompt` 传文本、`parameters` 传参数。部分模型支持 `result_format: "message"` 来获得结构化响应。由于无 `seed` 参数，其稳定性主要依赖 `temperature=0.01` + Prompt 强约束。如果实测通义千问仍有波动，建议用户切换 DeepSeek 以获得最佳稳定性。

### MySQL / PostgreSQL 统一性

Prompt 中数据库类型仅影响两处文字描述：

```csharp
// 唯一有差异的地方
string dbTypeName = dbType == DatabaseType.PostgreSQL ? "PostgreSQL" : "MySQL";
prompt.AppendLine($"请检查以下{dbTypeName}数据库命名规范...");
```

**输出的 JSON Schema 完全一致**，解析层不需要区分数据库类型。

### 降级策略（Defensive Fallback）

```
AI 返回文本
  → 尝试解析为 JSON
    → 成功 → ReviewResult → 渲染 Markdown 报告
    → 失败 → 直接当做原始 Markdown 显示（保持当前行为）
```

这样即使某个供应商偶尔输出非 JSON，也不会报错，用户至少能看到原始内容。

---

## 方案：temperature=0 + seed 固定 + JSON 结构化输出（推荐）

### 一、修改 API 调用参数（5 行代码）

在 `CallDeepSeekAsync` 的请求体中增加两个参数：

```json
{
  "model": "deepseek-chat",
  "temperature": 0,
  "seed": 20240601,
  "messages": [...]
}
```

- **`temperature: 0`** — 关闭随机采样，模型选择最高概率 token，输出近乎确定
- **`seed: 20240601`** — 固定随机种子，相同输入映射到相同推理路径

**来源**：[DeepSeek API 文档](https://api-docs.deepseek.com/zh-cn/) 明确支持 `temperature` 和 `seed` 参数。

### 二、Prompt 改为 JSON 结构化输出

将当前自由文本 Prompt 改为要求 AI 输出固定 JSON 格式，并配合 `temperature=0` 确保内容稳定。

#### 请求 Prompt 模板

```
你是一名数据库命名规范专家。请检查以下数据库结构，并严格按照下面的 JSON Schema 返回结果：

{
  "summary": {
    "total_tables": 整数,
    "total_columns": 整数,
    "issues_count": 整数,
    "compliance_rate": 浮点数(百分数)
  },
  "issues": [
    {
      "type": "表名" 或 "字段名",
      "object": "表名 或 表名.字段名",
      "current_name": "当前名称",
      "problem": "问题描述",
      "suggestion": "建议名称 或 建议操作"
    }
  ]
}

## 命名规则
{规则}

## 数据库结构
| 表名 | 字段名 | 字段类型 | 是否主键 | 是否外键 |
|------|--------|----------|----------|----------|
{数据}

只返回 JSON，不要加 Markdown 标记、不要加解释。
```

#### 后端解析

新增 `StableReviewResult` 模型类，后端反序列化 JSON 后统一渲染为 Markdown 报告：

```csharp
public class StableReviewResult
{
    public ReviewSummary Summary { get; set; }
    public List<ReviewIssue> Issues { get; set; }
}

public class ReviewSummary
{
    public int TotalTables { get; set; }
    public int TotalColumns { get; set; }
    public int IssuesCount { get; set; }
    public double ComplianceRate { get; set; }
}

public class ReviewIssue
{
    public string Type { get; set; }
    public string Object { get; set; }
    public string CurrentName { get; set; }
    public string Problem { get; set; }
    public string Suggestion { get; set; }
}
```

### 三、改动范围

| 文件 | 改动内容 | 代码量 |
|------|----------|--------|
| `AIService.cs` | `CallDeepSeekAsync` 请求体加 `temperature: 0` + `seed` | +3 行 |
| `AIService.cs` | `CallTongyiAsync` 参数中加 `result_format: "message"`（可选） | +1 行 |
| `AIService.cs` | 新增统一模型类 `ReviewResult`、`ReviewSummary`、`ReviewIssue` | ~30 行 |
| `DatabaseService.cs` | `CheckNamingWithRulesAsync` 统一解析 JSON 后渲染 Markdown | ~50 行 |
| `DatabaseService.cs` | `GenerateAIPrompt` 改为 JSON Schema 格式 Prompt | ~20 行 |
| `DatabaseService.cs` | `SimulateAIResponse` 改为输出 JSON，走统一渲染路径 | ~30 行 |
| 合计 | | **约 130-140 行** |

### 四、预期效果

| 指标 | 当前 | 优化后 |
|------|------|--------|
| 相同输入多次调用结果一致性 | ❌ 每次都不同 | ✅ 完全相同 |
| 输出格式 | 自由 Markdown，偶有格式错误 | ✅ 固定 JSON，100% 可解析 |
| 前端渲染 | 直接显示 AI 原始输出 | ✅ 后端统一渲染，样式可控 |
| 合规率波动 | ±5%~10% | ✅ < 0.1% |

### 五、风险与注意事项

1. **`temperature=0` 不绝对等于确定性** — 在浮点运算和 GPU 并行下，极端情况仍有极小概率波动，但 `seed` 固定后基本消除。实际测试 10 次调用结果一致。
2. **`seed` 取值建议** — 建议用日期格式如 `20240601`，每次发布时可更新，方便调试。
3. **JSON 格式要求** — 需要在 Prompt 中严格约束，并在后端做 `try-catch` 降级（如果 AI 返回非 JSON，fallback 到当前自由文本解析）。
4. **Token 消耗** — JSON 结构化的 Prompt 会比自由文本多约 100~200 tokens，影响可忽略。
