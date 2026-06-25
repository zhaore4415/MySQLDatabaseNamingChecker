# AI 审查结果持久化与健壮性分方案

## 1. 审查结果历史跟踪

### 1.1 目标

将每次 AI 审查的完整结果存入数据库，支持：
- 按仓库、时间、开发者维度查询历史审查记录
- 追踪仓库代码质量趋势（得分变化曲线）
- 追溯具体某次 PR 的审查详情

### 1.2 数据模型设计

```sql
-- 审查记录主表
CREATE TABLE ai_review_records (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    repo            VARCHAR(255) NOT NULL COMMENT '仓库全名，如 Dotnet/SJZY.CAMS.Api',
    pr_number       INT NOT NULL COMMENT 'PR 编号',
    source_branch   VARCHAR(100) NOT NULL COMMENT '源分支',
    target_branch   VARCHAR(100) NOT NULL COMMENT '目标分支',
    commit_sha      VARCHAR(40) NOT NULL COMMENT '触发审查的 commit SHA',
    review_type     ENUM('code','sql') NOT NULL COMMENT '审查类型',
    score           INT COMMENT 'AI 评分 0-100',
    total_issues    INT DEFAULT 0 COMMENT '发现问题总数',
    report_markdown TEXT COMMENT 'AI 生成的 Markdown 报告',
    ai_model        VARCHAR(50) COMMENT '使用的 AI 模型，如 qwen-max',
    token_used      INT COMMENT '消耗的 Token 数（如 AI 返回）',
    duration_ms     INT COMMENT '审查耗时（毫秒）',
    reviewer        VARCHAR(100) COMMENT '提交者用户名',
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_repo_time (repo, created_at),
    INDEX idx_pr (repo, pr_number),
    INDEX idx_reviewer (reviewer, created_at)
);

-- 审查问题明细表（可选，如需逐条追踪）
CREATE TABLE ai_review_issues (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    review_id       BIGINT NOT NULL,
    file_path       VARCHAR(500) NOT NULL COMMENT '问题所在文件',
    line_number     INT COMMENT '问题行号',
    severity        ENUM('error','warning','info') DEFAULT 'warning',
    category        VARCHAR(50) COMMENT '问题分类，如 锁表风险/缺索引',
    message         TEXT COMMENT '问题描述',
    suggestion      TEXT COMMENT '改进建议',
    
    FOREIGN KEY (review_id) REFERENCES ai_review_records(id) ON DELETE CASCADE,
    INDEX idx_review (review_id)
);
```

### 1.3 接口扩展

在现有 `/api/review` 返回结果后，由 Gitea Actions 调用新增接口上报：

```http
POST /api/review/report
Content-Type: application/json
Authorization: Bearer {token}

{
  "repo": "Dotnet/SJZY.CAMS.Api",
  "pr_number": 123,
  "source_branch": "feature/xxx",
  "target_branch": "main",
  "commit_sha": "a1b2c3d...",
  "review_type": "sql",
  "score": 85,
  "total_issues": 3,
  "report_markdown": "## 检查报告...",
  "ai_model": "qwen-max",
  "token_used": 15234,
  "duration_ms": 4200,
  "reviewer": "zhaohuan",
  "issues": [
    {
      "file_path": "docs/database/sql脚本/xxx.sql",
      "line_number": 15,
      "severity": "warning",
      "category": "缺索引",
      "message": "CustomerId 字段缺少索引",
      "suggestion": "建议添加 idx_customer_id"
    }
  ]
}
```

### 1.4 页面展示设计

新增导航栏入口：`ReviewHistory`（审查历史），与现有的"首页"、"数据库检查"并列。基于现有 UI 风格（Bootstrap 5 + Font Awesome + 渐变卡片）设计，保持视觉统一。

#### 页面 1：/ReviewHistory（总览看板）

**布局：顶部筛选区 + 左右双栏**

```
┌─────────────────────────────────────────────────────┐
│ 筛选区：仓库选择下拉框 | 日期范围 | 审查类型 | 搜索 │
├──────────────────────────┬──────────────────────────┤
│  左栏：仓库排名 TOP 10   │  右栏：个人排名 TOP 10   │
│  ┌─────┐                 │  ┌─────┐                 │
│  │ 1   │ 仓库名      99.5│  │ 1   │ 用户名    100  │
│  │ 2   │ 仓库名      98.2│  │ 2   │ 用户名    95.3 │
│  │ ... │                 │  │ ... │                 │
│  └─────┘                 │  └─────┘                 │
├──────────────────────────┴──────────────────────────┤
│ 近期审查记录列表（表格）                              │
│ 仓库 | PR | 类型 | 得分 | 问题数 | 提交者 | 时间    │
└─────────────────────────────────────────────────────┘
```

**关键交互：**
- 点击仓库名 → 进入单仓库详情页
- 点击 PR 编号 → 进入单次审查详情
- 得分用颜色标签：≥90 绿色、60-89 橙色、<60 红色
- 统计卡片复用现有 `stats-card` 渐变样式（`linear-gradient(135deg, #667eea 0%, #764ba2 100%)`）

#### 页面 2：/ReviewHistory/{repo}（单仓库详情）

**布局：3 个统计卡片 + 趋势图 + 记录列表**

```
┌─────────────────────────────────────────────────────┐
│ 仓库：SJZY.CAMS.Api                    [返回]      │
├─────────────────────────────────────────────────────┤
│ ┌──────────┐  ┌──────────┐  ┌──────────┐           │
│ │ 平均得分 │  │ 健壮性分 │  │ 近30天   │           │
│ │   94.2   │  │   92.8   │  │  12 次   │           │
│ └──────────┘  └──────────┘  └──────────┘           │
├─────────────────────────────────────────────────────┤
│ [得分趋势图]  [问题数趋势图]  (Chart.js 双折线图)   │
├─────────────────────────────────────────────────────┤
│ 该仓库审查记录（分页表格）                            │
│ 带展开行：点击展开查看 AI 报告摘要                    │
└─────────────────────────────────────────────────────┘
```

**趋势图：**
- X 轴：时间（近 30 天）
- Y 轴：左侧平均分（0-100），右侧问题数
- 双折线图：得分线 + 问题数柱状图

#### 页面 3：/ReviewHistory/{repo}/{pr_number}（单次审查详情）

**布局：左右分栏**

```
┌─────────────────────────────────────────────────────┐
│ PR #123 审查详情                         [返回]    │
├──────────────────────────┬──────────────────────────┤
│ 左栏：元信息             │ 右栏：AI 报告（Markdown）│
│ ┌────────────────────┐   │                         │
│ │ 仓库：SJZY.CAMS.Api│   │  ## AI 代码审查报告   │
│ │ PR：#123           │   │  **评分: 85/100**      │
│ │ 提交者：zhaohuan   │   │  **问题数: 3**         │
│ │ 类型：SQL          │   │                        │
│ │ 得分：85           │   │  1. 缺索引...          │
│ │ 问题数：3           │   │  2. 锁表风险...        │
│ │ 耗时：4.2s         │   │  3. 字段类型...        │
│ │ Token：15,234      │   │                        │
│ │ 时间：2026-06-22   │   │                        │
│ └────────────────────┘   │                         │
│ [查看 Gitea PR] [查看 CI │                         │
│  日志]                   │                         │
└──────────────────────────┴──────────────────────────┘
```

#### 技术选型

| 组件 | 建议 | 理由 |
|------|------|------|
| 图表 | Chart.js | 现有项目已用 Bootstrap，Chart.js 轻量兼容好 |
| Markdown 渲染 | marked.js | 前端直接渲染 AI 返回的 Markdown 报告 |
| 表格排序/筛选 | 手写 JS 或 Bootstrap Table | 不需要引入 DataTables 重型库 |
| 日期选择 | 原生 `<input type="date">` | 最轻量，无需额外库 |

### 1.5 实现步骤

1. **Week 1**：添加数据库表结构（EF Core 迁移）
2. **Week 2**：新增 `/api/review/report` 上报接口
3. **Week 3**：修改 Gitea Actions `ai-review.yml`，在最后一步调用 `/api/review/report`
4. **Week 4**：新增 Web 页面展示历史记录（可选）

---

## 2. 健壮性分（加权评分机制）

### 2.1 核心问题

简单平均分的问题：
- 开发者 A：提交 10 次，每次 95 分 → 平均 95 分
- 开发者 B：提交 9 次 95 分 + 1 次 40 分 → 平均 89.5 分

但后者有一次 40 分的低质量提交，对系统的潜在危害比 89.5 分看起来更糟。单纯平均分无法体现这一点。

### 2.2 加权机制设计

参考截图中的系统，引入**按单次评分加权**的健壮性分：

| 单次评分区间 | 权重 | 说明 |
|-------------|------|------|
| ≥ 90 分 | 100% | 优秀代码，完全认可 |
| 80-89 分 | 90% | 良好，轻微降权 |
| 60-79 分 | 80% | 及格，风险代码，较大降权 |
| < 60 分 | 50% | 不合格，严重风险，大幅降权 |

### 2.3 计算公式

**个人健壮性分**：

```
个人总分 = Σ(单次评分 × 权重) / Σ(权重)
```

**示例对比**：

| 场景 | 提交记录 | 简单平均分 | 健壮性分 | 差异 |
|------|---------|-----------|---------|------|
| 场景 A | 10 次 × 95 分 | 95.0 | 95.0 | 无 |
| 场景 B | 9 次 × 95 分 + 1 次 × 40 分 | 89.5 | **87.5** | 下降 2.0 分 |
| 场景 C | 5 次 × 95 分 + 5 次 × 60 分 | 77.5 | **71.5** | 下降 6.0 分 |
| 场景 D | 1 次 × 95 分 + 9 次 × 50 分 | 54.5 | **32.0** | 下降 22.5 分 |

**结论**：健壮性分对低质量提交更敏感，越多的低分提交，总分下降越明显。公式使用 `Σ(评分×权重)/总次数`（除以总次数而非权重和），确保低分被真正惩罚。

**计算公式**：

```
个人总分 = Σ(单次评分 × 权重) / 总次数
```

> 注意：除以 `总次数` 而不是 `Σ(权重)`。如果除以权重和，分母变小，加权平均反而可能高于简单平均，与"惩罚低分"的设计意图相悖。

### 2.4 仓库级与个人级排名

| 维度 | 计算方式 | 展示 |
|------|---------|------|
| **仓库评分** | 该仓库近 30 天所有提交的平均分（可加权） | 项目排名看板 |
| **个人健壮性分** | 该开发者近 30 天所有提交的个人总分 | 个人排名看板 |
| **团队趋势** | 按周/月统计仓库平均分变化 | 折线图 |

### 2.5 数据表扩展

```sql
-- 仓库统计汇总表（每日/每周更新，加速查询）
CREATE TABLE ai_review_repo_stats (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    repo            VARCHAR(255) NOT NULL,
    stat_date       DATE NOT NULL,
    review_count    INT DEFAULT 0,
    avg_score       DECIMAL(5,2),
    robust_score    DECIMAL(5,2) COMMENT '加权后的健壮性分',
    total_issues    INT DEFAULT 0,
    
    UNIQUE INDEX uk_repo_date (repo, stat_date)
);

-- 个人统计汇总表
CREATE TABLE ai_review_user_stats (
    id              BIGINT PRIMARY KEY AUTO_INCREMENT,
    username        VARCHAR(100) NOT NULL,
    repo            VARCHAR(255) NOT NULL,
    stat_date       DATE NOT NULL,
    review_count    INT DEFAULT 0,
    avg_score       DECIMAL(5,2),
    robust_score    DECIMAL(5,2),
    total_issues    INT DEFAULT 0,
    
    UNIQUE INDEX uk_user_repo_date (username, repo, stat_date)
);
```

### 2.6 实现步骤

1. **Week 1**：在现有 `ai_review_records` 表上增加 `robust_score` 计算字段
2. **Week 2**：开发后台统计任务（每日凌晨汇总近 30 天数据）
3. **Week 3**：新增 API 接口 `/api/stats/repo/{repo}` 和 `/api/stats/user/{user}`
4. **Week 4**：前端页面展示排名与趋势图（可选）

---

## 3. 与现有系统的集成关系

```
PR 提交/更新
    ↓
Gitea Actions（ai-review.yml）
    ↓
调用 /api/review（现有功能）→ AI 审查
    ↓
Gitea Actions 收到结果
    ↓
  ├─ 评论到 PR（现有功能）
  ├─ 调用 /api/review/report（新增）→ 数据入库
  └─ 触发后台统计更新（新增）
    ↓
Web 看板展示历史记录与排名（新增）
```

---

## 4. 决策建议

| 功能 | 优先级 | 工作量 | 建议 |
|------|--------|--------|------|
| 审查结果入库 | 高 | 小 | 先做这个，数据是一切的基础 |
| 健壮性分计算 | 中 | 小 | 依赖入库数据，做统计任务即可 |
| Web 看板（3 个页面） | 低 | 中 | 有数据后，看板随时可以补，可用 Grafana 临时替代 |
| 问题明细表 | 低 | 小 | 可选，如需逐条分析才做 |

**推荐落地顺序**：
1. 数据库表结构（1 天）
2. `/api/review/report` 上报接口（1 天）
3. 修改 Gitea Actions 调用上报（0.5 天）
4. 健壮性分统计任务（1 天）
5. 简单的 Web 列表页展示（2-3 天）

总计约 **1 周**可完成 MVP。

**前端页面渐进式实现建议**：
1. **第 1-2 天**：`/ReviewHistory` 列表页（只展示表格，不做排名）
2. **第 3-4 天**：单仓库详情页（统计卡片 + 记录列表）
3. **第 5-6 天**：趋势图 + 排名看板
4. **第 7 天**：单次审查详情页

---

*文档版本: 1.1*  
*创建日期: 2026-06-22*  
*最后更新: 2026-06-22*
