# PostgreSQL ColumnType 显示优化方案

## 问题

PostgreSQL 表结构提取时，`ColumnType` 字段显示异常：

```
实际数据库类型       →   当前显示的 ColumnType
─────────────────────────────────────────────
varchar(16)[]        →   _varchar        ❌
integer              →   integer          ✅（恰好一致）
timestamp            →   timestamp        ✅
numeric(10,2)        →   numeric          ⚠️ 丢了精度
varchar(32)          →   varchar          ⚠️ 丢了长度
```

**根因**：MySQL 的 `information_schema.COLUMNS` 有 `COLUMN_TYPE` 列（返回 `varchar(16)`），但 **PostgreSQL 没有这个列**。当前代码用 `UDT_NAME` 做 fallback，只拿到了裸类型名（`_varchar`、`int4` 等内部名）。

## 方案：直接用 `pg_catalog.format_type()`

### 一、为什么不用 C# 拼接？

PostgreSQL 内置函数 `pg_catalog.format_type(a.atttypid, a.atttypmod)` **直接返回完整的、可读的类型字符串**，包括长度、精度、数组标记等，一条 SQL 搞定，零 C# 拼接。

| 实际类型 | `format_type()` 返回值 |
|----------|----------------------|
| `varchar(16)[]` | `varchar(16)[]` ✅ |
| `varchar(32)` | `character varying(32)` ✅ |
| `numeric(10,2)` | `numeric(10,2)` ✅ |
| `timestamp with time zone` | `timestamp with time zone` ✅ |
| `integer` | `integer` ✅ |

### 二、SQL 改用 `pg_catalog.pg_attribute`

将当前的 columns 查询从 `information_schema.COLUMNS` 改为 `pg_catalog.pg_attribute` + `pg_catalog.pg_class` + `pg_catalog.pg_namespace`：

```sql
SELECT
    c.relname AS TABLE_NAME,
    a.attname AS COLUMN_NAME,
    pg_catalog.format_type(a.atttypid, a.atttypmod) AS COLUMN_TYPE,
    a.attnotnull AS IS_NULLABLE,
    CASE WHEN a.attnum = ANY(ix.indkey) THEN 'PRI' ELSE '' END AS COLUMN_KEY
FROM pg_catalog.pg_class c
JOIN pg_catalog.pg_namespace n ON c.relnamespace = n.oid
JOIN pg_catalog.pg_attribute a ON a.attrelid = c.oid
LEFT JOIN pg_catalog.pg_index ix ON ix.indrelid = c.oid AND ix.indisprimary
WHERE n.nspname = $1
  AND c.relkind = 'r'          -- 只取普通表
  AND a.attnum > 0             -- 排除系统列
  AND NOT a.attisdropped       -- 排除已删除列
ORDER BY c.relname, a.attnum
```

同时需要配合主键索引查询来标记 `COLUMN_KEY`（当前使用 `information_schema.TABLE_CONSTRAINTS` 的方式也可以保留，看实际情况）。

但有一个简化方案——只改 `ColumnType` 这一列，其他保持原样。

### 三、最小改动方案（推荐）

不改整个查询，**只改 `COLUMN_TYPE` 那一列**——在当前的 `columnsSql` 里把 `c.UDT_NAME AS COLUMN_TYPE` 替换为一个子查询或改用 `pg_catalog.format_type()` 即可。

但 `information_schema.COLUMNS` 里没有直接等价的 `format_type()` 调用，因为 `information_schema` 不暴露 `atttypid` 和 `atttypmod`。

所以**实际可行的最小改动**是：整段 columns 查询改为 `pg_catalog` 方式，SQL 返回的列名保持一致，C# 读取代码不变。

### 四、SQL 改动（完整版）

```sql
-- 替换当前的 columnsSql（支持主键标记）
WITH primary_keys AS (
    SELECT
        ix.indrelid,
        unnest(ix.indkey) AS attnum
    FROM pg_catalog.pg_index ix
    WHERE ix.indisprimary
)
SELECT
    c.relname::text AS TABLE_NAME,
    a.attname::text AS COLUMN_NAME,
    pg_catalog.format_type(a.atttypid, a.atttypmod) AS COLUMN_TYPE,
    CASE WHEN a.attnotnull THEN 'NO' ELSE 'YES' END AS IS_NULLABLE,
    a.attnum,
    CASE WHEN pk.attnum IS NOT NULL THEN 'PRIMARY KEY' ELSE '' END AS COLUMN_KEY
FROM pg_catalog.pg_class c
JOIN pg_catalog.pg_namespace n ON c.relnamespace = n.oid
JOIN pg_catalog.pg_attribute a ON a.attrelid = c.oid
LEFT JOIN primary_keys pk ON pk.indrelid = c.oid AND pk.attnum = a.attnum
WHERE n.nspname = $1
  AND c.relkind = 'r'
  AND a.attnum > 0
  AND NOT a.attisdropped
ORDER BY c.relname, a.attnum
```

### 五、C# 读取代码改动

当前代码：
```csharp
DataType = reader["DATA_TYPE"].ToString(),
ColumnType = reader["COLUMN_TYPE"]?.ToString() ?? reader["DATA_TYPE"].ToString(),
```

改为：
```csharp
DataType = reader["DATA_TYPE"].ToString(),  // 其实用不上了，保留兼容
ColumnType = reader["COLUMN_TYPE"].ToString(),
```

**其他字段**（`IsNullable`、`ColumnKey` 等）读取方式不变，列名保持一致。

### 六、注意事项

1. **`IS_NULLABLE`** — `pg_attribute.attnotnull` 是 `bool`，`true` = NOT NULL（MySQL 风格返回 `"YES"/"NO"`，需用 `CASE WHEN` 转换）
2. **`COLUMN_DEFAULT`** — 当前查询未用到 `c.COLUMN_DEFAULT`，新查询也暂不取，需要时可以从 `pg_attrdef` 表补充
3. **`format_type()` 返回 `character varying(n)` 而非 `varchar(n)`** — 两者是等价的，对 AI 来说不影响理解。如果一定想要 `varchar` 简写，可以在 C# 里做 `replace("character varying", "varchar")`
4. **不影响 MySQL** — 只改 `GetPostgreSQLDatabaseSchemaAsync` 方法，MySQL 路径完全不变

### 七、效果对比

| 实际数据库 | 当前 | 修复后 |
|-----------|------|--------|
| `varchar(16)[]` | `_varchar` | `varchar(16)[]` 或 `character varying(16)[]` ✅ |
| `varchar(32)` | `varchar` | `character varying(32)` ✅ |
| `numeric(10,2)` | `numeric` | `numeric(10,2)` ✅ |
| `timestamp with time zone` | `timestamptz` | `timestamp with time zone` ✅ |
| `integer` | `int4` | `integer` ✅ |

### 八、改动范围

| 文件 | 改动内容 | 代码量 |
|------|----------|--------|
| `DatabaseService.cs` | `columnsSql` 替换为 `pg_catalog` 查询 | ~30 行 |
| `DatabaseService.cs` | C# 侧修改 `ColumnType` / `IsNullable` 读取逻辑 | ~5 行 |
| 合计 | | **~35 行** |
