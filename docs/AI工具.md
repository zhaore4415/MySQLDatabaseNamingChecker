# AI 通用检查工具技术方案（精简版）

## 1. 项目概述

### 1.1 背景
当前项目 `DBCheckAI` 仅支持 MySQL/PostgreSQL 数据库命名规范检查。为提升研发团队代码质量，计划拓展为 **AI 通用检查工具**，覆盖代码审查、数据库脚本检查等场景，并集成到 CI/CD 流程中。

### 1.2 目标
- 提供统一的 AI 驱动的代码质量检查服务
- 支持 Gitea Actions 集成，在 PR/MR 阶段自动检查
- 输出可操作的改进建议，提升代码可维护性
- **现有 Web UI 功能完全保留，不做任何改动**

### 1.3 适用范围（第一阶段）
- **代码审查**：仅 C#（后端代码）
- **数据库脚本**：SQL、迁移脚本（`.sql` 文件）
- **不做**：前端、配置文件、其他语言

---

## 2. 核心原则：极简，不引入新基础设施

本方案刻意做减法，以下设计**全部砍掉**：

| 砍掉的设计 | 原因 |
|-----------|------|
| Webhook 自动触发 | 需要公网暴露、签名验证、事件处理，太重。CI 里直接 `curl` 调用 API 即可 |
| SARIF 输出格式 | 团队没有 SARIF 消费工具链，直接返回 JSON + Markdown 即可 |
| JSON 规则引擎 | 检查维度直接**硬编码在 Prompt 中**，不引入规则解析器 |
| 任务队列（Redis/内存） | 先走同步接口，AI 响应慢就设长超时（5min）。后续真有瓶颈再说 |
| 数据库存储历史记录 | 先用文件日志或内存，不需要 SQLite/PostgreSQL 存储检查历史 |
| 多语言支持（Go、Java 等） | 第一阶段只支持 C# 和 SQL |
| 复杂 API 版本控制（v1/v2） | 只提供一个 `/api/review` 接口，够用即可 |

**保留复用的**：
- `ASP.NET Core 8.0` 后端（现有基础）
- `IAIService` / `AIService`（通义千问/DeepSeek 已打通）
- 现有数据库命名规范 Web UI（完全不动）

---

## 3. 系统架构

### 3.1 整体流程

```
Gitea 仓库 PR 提交/更新
    ↓
Gitea Actions 触发（新增 ai-review.yml）
    ↓
    1. git diff 提取变更文件（仅 .cs 和 .sql）
    2. 读取文件完整内容
    3. curl 调用 DBCheckAI 外部 API
    ↓
DBCheckAI 服务（现有项目扩展）
    ↓
    根据 type 调用不同 Prompt（代码/SQL）
    → 复用 IAIService 调用通义/DeepSeek
    ↓
返回 JSON 结果（score + issues + markdown）
    ↓
Gitea Actions 收到结果
    ↓
    1. 打印评分和 Markdown 到 CI 日志
    2. 可选：评论到 PR
    3. 可选：合并门禁（默认不启用）
```

### 3.2 技术栈

| 层级 | 技术选型 | 说明 |
|------|---------|------|
| 后端框架 | ASP.NET Core 8.0 | 现有基础，继续沿用 |
| API 形式 | Minimal API（或 Controller） | 在现有 Program.cs 中扩展 |
| AI 服务 | 通义千问 / DeepSeek | 已集成，复用现有 IAIService |
| 鉴权 | HTTP Header Token | 简单 Bearer Token 校验，无需 OAuth |
| 报告格式 | JSON + Markdown | 最简洁，CI 直接消费 |

---

## 4. API 接口设计（极简）

### 4.1 唯一接口

只提供一个接口，通过 `type` 字段区分代码审查和 SQL 审查。

```http
POST /api/review
Content-Type: application/json
Authorization: Bearer {token}

{
  "type": "code",           // "code" 或 "sql"
  "repo": "org/repo-name",
  "pr_number": 123,
  "files": [
    {
      "path": "src/Services/OrderService.cs",
      "content": "using System; ...",     // 纯文本内容
      "change_type": "modified"
    }
  ]
}
```

**响应：**

```json
{
  "score": 72,
  "total_issues": 3,
  "issues": [
    {
      "file": "src/Services/OrderService.cs",
      "line": 45,
      "severity": "warning",
      "category": "可维护性",
      "message": "方法超过 100 行，建议拆分",
      "suggestion": "拆分为 ValidateOrder() + SaveOrder() 两个方法"
    }
  ],
  "report_markdown": "## 检查报告\n..."
}
```

### 4.2 鉴权设计

- 简单 HTTP Header 校验：`Authorization: Bearer {token}`
- Token 在 `appsettings.json` 中配置，CI 通过 Gitea Secret 传入
- 无需 OAuth、无需 JWT、无需用户系统

---

## 5. 输入内容策略：完整文件 vs Git Diff

### 5.1 方案对比

| 方案 | 优点 | 缺点 | 结论 |
|------|------|------|------|
| **完整文件内容** | AI 有完整上下文，理解准确；实现简单 | Token 消耗大；大文件可能超时 | **第一阶段采用** |
| **Git Diff 片段** | Token 消耗小；只传变更部分 | AI 缺少上下文，容易误判；需要解析 diff 格式 | **后续按需切换** |

### 5.2 当前决策

**第一阶段采用「完整文件内容」**，理由：
- PR 通常只涉及少量文件（3-10 个），总 Token 可控
- AI 对 diff 格式的理解不稳定，容易漏判
- 实现成本最低，不需要写 diff 解析逻辑

**兜底策略**（防止卡死）：
- 单文件大小限制：最大 50KB，超过则跳过并在日志中提示
- 单次请求总 Token 限制：如果文件过大，返回 `{"score": null, "error": "文件过大，建议拆分提交"}`
- CI 超时：curl 设置 `--max-time 300`（5 分钟）
- 并发限制：如果后续用户量上来，在 API 层加简单并发数限制（如同时最多 5 个检查请求）

**后续优化方向**（如果 Token 成本或延迟成为问题）：
- 对超大文件（> 50KB），只传 `git diff` 片段
- 引入缓存：相同文件哈希的检查结果缓存 1 小时
- 考虑分块：把大文件拆分成多个 class/method 分别检查

---

## 6. AI Prompt 设计（硬编码，无规则引擎）

不引入 JSON 规则引擎，直接在代码里写死 Prompt 模板。

### 6.1 代码审查 Prompt

```
你是一名资深 C# 架构师。请对以下代码变更进行审查。

评估维度（每项 0-20 分，总分 100）：
1. 设计模式符合度（SOLID 原则、常用设计模式应用）
2. 代码复用性（是否有重复代码、DRY 原则遵循）
3. 架构合理性（分层清晰、依赖关系合理、耦合度）
4. 可维护性（命名规范、可读性、注释完整性）
5. 最佳实践遵循度（C# idiomatic 写法、.NET 框架最佳实践）

请按以下 JSON 格式输出，不要输出任何其他解释：
{
  "score": 85,
  "issues": [
    {
      "file": "文件路径",
      "line": 行号,
      "severity": "warning|info",
      "category": "设计模式|代码复用|架构|可维护性|最佳实践",
      "message": "问题描述",
      "suggestion": "改进建议"
    }
  ]
}

待审查代码：
{文件内容}
```

### 6.2 SQL 审查 Prompt

```
你是一名数据库专家。请审查以下 SQL 脚本。

检查维度：
1. 命名规范（snake_case、保留字、审计字段完整性）
2. 脚本优化（慢 SQL 风险、N+1 查询、大事务）
3. 静态分析（缺索引、全表扫描、批量操作在循环内）

请按以下 JSON 格式输出，不要输出任何其他解释：
{
  "score": 85,
  "issues": [
    {
      "file": "文件路径",
      "line": 行号,
      "severity": "warning|info",
      "category": "命名规范|脚本优化|静态分析",
      "message": "问题描述",
      "suggestion": "改进建议"
    }
  ]
}

待审查 SQL：
{文件内容}
```

---

## 7. Gitea Actions 集成

### 7.1 Workflow 文件

在仓库 `.gitea/workflows/` 下新建 `ai-review.yml`：

```yaml
name: AI Code & SQL Review

on:
  pull_request:
    types: [opened, synchronize]

jobs:
  ai-review:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      # 1. 提取变更文件（仅 .cs 和 .sql）
      - name: Prepare changed files
        id: changes
        run: |
          git diff --name-only origin/${{ github.base_ref }}...HEAD | grep -E '\.(cs|sql)$' > changed_files.txt || true
          if [ ! -s changed_files.txt ]; then
            echo "has_changes=false" >> $GITHUB_OUTPUT
            echo "No .cs or .sql files changed, skipping AI review."
          else
            echo "has_changes=true" >> $GITHUB_OUTPUT
          fi

      # 2. 构建请求体（读取完整文件内容）
      - name: Build review payload
        if: steps.changes.outputs.has_changes == 'true'
        id: payload
        run: |
          FILES_JSON="["
          while IFS= read -r file; do
            if [ -f "$file" ]; then
              # 简单转义，避免 JSON 解析失败
              CONTENT=$(cat "$file" | sed 's/\\/\\\\/g' | sed 's/"/\\"/g' | tr '\n' ' ' | sed 's/  */ /g')
              FILES_JSON="${FILES_JSON}{\"path\":\"$file\",\"content\":\"$CONTENT\",\"change_type\":\"modified\"},"
            fi
          done < changed_files.txt
          FILES_JSON=$(echo "$FILES_JSON" | sed 's/,$//')
          FILES_JSON="${FILES_JSON}]"
          echo "{\"type\":\"code\",\"repo\":\"${{ github.repository }}\",\"pr_number\":${{ github.event.number }},\"files\":$FILES_JSON}" > request.json
          echo "payload_file=request.json" >> $GITHUB_OUTPUT

      # 3. 调用 AI Review API
      - name: Call AI Review API
        if: steps.changes.outputs.has_changes == 'true'
        id: ai_call
        run: |
          RESPONSE=$(curl -s -X POST "${{ secrets.AI_REVIEW_API_URL }}/api/review" \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer ${{ secrets.AI_REVIEW_TOKEN }}" \
            -d @${{ steps.payload.outputs.payload_file }} \
            --max-time 300)
          echo "result=$RESPONSE" >> $GITHUB_OUTPUT
          echo "$RESPONSE" | jq '.' || echo "$RESPONSE"

      # 4. 打印结果到 CI 日志
      - name: Report results
        if: steps.changes.outputs.has_changes == 'true'
        run: |
          SCORE=$(echo '${{ steps.ai_call.outputs.result }}' | jq -r '.score // 0')
          ISSUES=$(echo '${{ steps.ai_call.outputs.result }}' | jq -r '.total_issues // 0')
          echo "========================================"
          echo "AI 审查评分: ${SCORE}/100"
          echo "发现问题: ${ISSUES} 个"
          echo "========================================"
          # 合并门禁：代码已保留，但默认不启用（直接 exit 0）
          # 如需强制，取消下面注释并改为 exit 1
          # if [ "$SCORE" -lt 60 ]; then
          #   echo "评分低于 60，标记失败"
          #   exit 1
          # fi
          exit 0

      # 5. 可选：将结果评论到 PR（需要 Gitea API Token）
      - name: Comment PR
        if: steps.changes.outputs.has_changes == 'true'
        uses: actions/github-script@v6
        with:
          script: |
            const result = JSON.parse('${{ steps.ai_call.outputs.result }}');
            const markdown = result.report_markdown || 'AI 检查完成';
            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: `## AI 代码审查报告\n\n**评分**: ${result.score}/100\n**问题数**: ${result.total_issues}\n\n${markdown}`
            });
```

### 7.2 Gitea Secret 配置

在 Gitea 仓库设置中配置：

| Secret | 值 | 说明 |
|--------|------|------|
| `AI_REVIEW_API_URL` | `http://你的DBCheckAI服务地址:端口` | 内网地址即可，Runner 能访问到 |
| `AI_REVIEW_TOKEN` | 你自己定的 API Key | 与 DBCheckAI 的 `appsettings.json` 中配置一致 |

### 7.3 合并门禁（实现但不启用）

- **实现**：CI 中已预留评分低于 60 就 `exit 1` 的逻辑（见 Workflow 第 4 步注释）
- **当前状态**：默认 `exit 0`，即**不阻塞合并**，仅作为参考
- **启用方式**：取消注释 `# if [ "$SCORE" -lt 60 ]; then ... exit 1` 即可
- **建议**：先观察 2 周，看 AI 评分的合理性和误报率，再决定是否强制

---

## 8. 实现计划（极简三阶段）

| 阶段 | 时间 | 目标 |
|------|------|------|
| **Week 1** | 3 天 | DBCheckAI 扩展 `/api/review` 接口 + 两个 Prompt（代码/SQL）+ 本地 Postman 测试 |
| **Week 2** | 2 天 | 编写 `ai-review.yml` + 在测试仓库跑通真实 PR 检查 |
| **Week 3** | 2 天 | 观察真实 PR 效果，微调 Prompt（这是最关键的一步）+ 添加单文件大小限制兜底 |

---

## 9. 风险评估

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| AI 响应慢（> 30s） | CI 体验差 | curl 设 5min 超时；CI 日志里打印"AI 检查进行中..." |
| AI 误报率高 | 开发者不信任 | 先不启用合并门禁，观察 2 周；根据反馈微调 Prompt |
| Token 消耗大 | 成本高 | 单文件 50KB 限制；PR 通常只有少量文件变更 |
| 大文件导致超时 | CI 失败 | 超过 50KB 直接跳过，返回提示；CI 里显示"文件过大已跳过" |
| 并发请求多 | 服务卡死 | 后续可在 API 层加简单并发限制（如 SemaphoreSlim 限制 5 个） |

---

## 10. 附录

### 10.1 与现有功能的关系

| 现有功能 | 处理方式 |
|---------|---------|
| 数据库命名规范 Web UI | **完全保留，不动** |
| `DatabaseService`（连接数据库检查结构） | 保留，后续 SQL 审查如需执行 `EXPLAIN` 可复用 |
| `IAIService` / `AIService` | 直接复用，新增 Prompt 构建器调用它 |
| `appsettings.json`（AI 配置） | 新增一个 `ApiToken` 字段用于 API 鉴权即可 |

### 10.2 参考资源

- [Gitea Actions 文档](https://docs.gitea.com/usage/actions/overview)
- [通义千问 API](https://help.aliyun.com/document_detail/2587494.html)
- [DeepSeek API](https://platform.deepseek.com/api-docs/)

---

*文档版本: 2.0*  
*创建日期: 2026-06-15*  
*最后更新: 2026-06-16*
