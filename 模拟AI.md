# 模拟AI 升级方案 (数据库命名规范 v2.0)

为了满足您更新后的 12 项数据库命名规范，我们需要对 `DatabaseService.cs` 中的“模拟AI”逻辑进行全面升级。以下是具体的技术实现方案。

## 1. 核心规则变更对比

| 序号 | 规则描述 | 当前状态 | 升级方案 |
| :--- | :--- | :--- | :--- |
| **8** | **新版审计属性** | 包含 `is_deleted` | **移除 `is_deleted`**，仅保留 `created_by/at`, `updated_by/at`, `deleted_by/at`。 |
| **10** | **唯一索引包含软删除** | 使用 `is_deleted` | **改为包含 `deleted_at`**。建议 `deleted_at` 默认为一个安全值（如 `1970-01-01`）以支持 `NOT NULL` 唯一索引。 |
| **6** | **唯一索引列数** | 未实现 | 检查每个唯一索引的组合列数，**超过 3 列时报错**。 |
| **7** | **唯一索引非空** | 未实现 | 检查所有参与唯一索引的字段，若为 `NULL` 则报错。 |
| **5** | **禁止额外约束** | 部分实现 | 检查除 `PRIMARY KEY` 和 `UNIQUE INDEX` 外是否存在其他约束（如 `CHECK` 约束、外键约束）。 |
| **9** | **数据清洗表 `last_time`**| 未实现 | 识别带有 `cleaning` 或 `archive` 标识的表，检查其是否包含 `last_time` 字段。 |

## 2. 技术实现步骤

### 第一阶段：增强元数据提取 (Metadata API)
目前 `GetDatabaseSchemaAsync` 仅提取了列的基本信息。我们需要扩展它以获取索引信息：
- **MySQL**: 查询 `information_schema.STATISTICS` 以获取索引名、列名、是否唯一。
- **PostgreSQL**: 查询 `pg_indexes` 和 `pg_attribute` 关联获取索引详情。

### 第二阶段：规则逻辑重写
1.  **审计属性更新**：
    - 更新 `newAuditColumns` 集合，移除 `is_deleted`。
    - 在检查时，如果发现 `is_deleted` 字段，建议将其移除并迁移逻辑。
2.  **索引深度检查 (Rule 6, 7, 10)**：
    - 实现一个新的 `ValidateIndexes` 子方法。
    - 遍历每个表的索引：如果 `INDEX_NAME` 包含 `uk_` 或 `unique`，则验证其列数 ≤ 3 且每列都是 `NOT NULL`。
    - 强制检查：必须包含 `deleted_at` 字段在该索引中。
3.  **SQL 修复语句更新**：
    - 自动生成添加 `deleted_at` 并将其包含在现有唯一索引中的 `ALTER TABLE` 语句。

## 3. 示例报告效果

### ❌ 不符合规范的命名
| 对象 | 当前名称 | 问题 | 建议修改 |
| :--- | :--- | :--- | :--- |
| `order_unique` | `order_no` | 唯一索引未包含 `deleted_at` | `(order_no, deleted_at)` |
| `user_profile` | `user_profile.zip` | 参与唯一索引的字段不允许为 NULL | 修改为 `NOT NULL` |
| `user_info` | `is_deleted` | 新版审计规范已移除此字段 | 删除此字段，使用 `deleted_at` |

## 4. 下一步行动
如果您认可此方案，我将开始：
1.  修改 `DbObject` 加入索引元数据。
2.  更新数据库查询逻辑以抓取索引。
3.  重构 `SimulateAIResponse` 核心算法。

---
> [!IMPORTANT]
> **关于 Rule 10 的技术建议**：如果将 `deleted_at` 放入唯一索引，建议将其类型设为 `DATETIME NOT NULL`，默认值为 `'1970-01-01 00:00:00'`。如果允许 `NULL`，在某些数据库引擎（如 MySQL）中，多个 `NULL` 不会被视为冲突，会导致软删除逻辑失效。
