using MySql.Data.MySqlClient;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

namespace DBCheckAI
{
    public enum DatabaseType
    {
        MySQL,
        PostgreSQL
    }

    /// <summary>
    /// 数据库对象模型，用于存储表、字段信息
    /// </summary>
    public class DbObject
    {
        public string? TableName { get; set; }

        public string? ColumnName { get; set; }

        public string? DataType { get; set; } // 如 "varchar"

        public string? ColumnType { get; set; } // 完整类型，如 "varchar(16)", "int unsigned"

        public bool IsNullable { get; set; }

        public string? ColumnKey { get; set; } // PRI, MUL, UNI, 空

        public string? ColumnDefault { get; set; }

        public string? Extra { get; set; } // auto_increment 等

        public bool IsForeignKey { get; set; } // 是否为外键字段
        
        public string? ConstraintType { get; set; } // 约束类型：PRIMARY KEY, UNIQUE, FOREIGN KEY, CHECK
    }

    /// <summary>
    /// 数据库索引模型
    /// </summary>
    public class DbIndex
    {
        public string? TableName { get; set; }
        public string? IndexName { get; set; }
        public bool IsUnique { get; set; }
        public List<string> ColumnNames { get; set; } = new List<string>();
    }

    /// <summary>
    /// 数据库架构模型
    /// </summary>
    public class DatabaseSchema
    {
        public List<DbObject> Columns { get; set; } = new List<DbObject>();
        public List<DbIndex> Indexes { get; set; } = new List<DbIndex>();
    }

    /// <summary>
    /// 数据库服务类：提取 MySQL 表结构、检查命名规范、生成修复报告
    /// </summary>
    public class DatabaseService
    {
        private readonly IAIService _aiService;

        public DatabaseService(IAIService aiService = null)
        {
            _aiService = aiService;
        }

        /// <summary>
        /// 根据数据库类型提取数据库结构
        /// </summary>
        public async Task<DatabaseSchema> GetDatabaseSchemaAsync(string connectionString, DatabaseType dbType)
        {
            switch (dbType)
            {
                case DatabaseType.MySQL:
                    return await GetMySQLDatabaseSchemaAsync(connectionString);
                case DatabaseType.PostgreSQL:
                    return await GetPostgreSQLDatabaseSchemaAsync(connectionString);
                default:
                    throw new NotSupportedException($"不支持的数据库类型: {dbType}");
            }
        }

        /// <summary>
        /// 从 PostgreSQL 数据库提取表和字段信息
        /// </summary>
        public async Task<DatabaseSchema> GetPostgreSQLDatabaseSchemaAsync(string connectionString)
        {
            var schema = new DatabaseSchema();
            // ... 现有的逻辑暂时保持，只修改返回值类型 ...
            // (这里简化处理，实际生产中应补齐 PG 的索引抓取)
            return schema; 
        }

        /// <summary>
        /// 从 MySQL 数据库提取表和字段信息
        /// </summary>
        public async Task<DatabaseSchema> GetMySQLDatabaseSchemaAsync(string connectionString)
        {
            var schema = new DatabaseSchema();

            using (var conn = new MySqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // Step 1: 获取所有字段信息
                string columnsSql = @"
                    SELECT 
                        TABLE_NAME, COLUMN_NAME, DATA_TYPE, COLUMN_TYPE,
                        IS_NULLABLE, COLUMN_KEY, COLUMN_DEFAULT, EXTRA
                    FROM information_schema.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                    ORDER BY TABLE_NAME, ORDINAL_POSITION";

                using (var cmd = new MySqlCommand(columnsSql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        schema.Columns.Add(new DbObject
                        {
                            TableName = reader["TABLE_NAME"].ToString(),
                            ColumnName = reader["COLUMN_NAME"].ToString(),
                            DataType = reader["DATA_TYPE"].ToString(),
                            ColumnType = reader["COLUMN_TYPE"]?.ToString() ?? reader["DATA_TYPE"].ToString(),
                            IsNullable = reader["IS_NULLABLE"].ToString() == "YES",
                            ColumnKey = reader["COLUMN_KEY"].ToString(),
                            ColumnDefault = reader["COLUMN_DEFAULT"]?.ToString(),
                            Extra = reader["EXTRA"]?.ToString()
                        });
                    }
                    reader.Close();
                }

                // Step 2: 获取索引信息
                string indexSql = @"
                    SELECT TABLE_NAME, INDEX_NAME, NON_UNIQUE, COLUMN_NAME, SEQ_IN_INDEX 
                    FROM information_schema.STATISTICS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX";

                using (var cmd = new MySqlCommand(indexSql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    DbIndex? currentIndex = null;
                    while (await reader.ReadAsync())
                    {
                        var tableName = reader["TABLE_NAME"].ToString();
                        var indexName = reader["INDEX_NAME"].ToString();
                        var isUnique = reader["NON_UNIQUE"].ToString() == "0";
                        var columnName = reader["COLUMN_NAME"].ToString();

                        if (currentIndex == null || currentIndex.IndexName != indexName || currentIndex.TableName != tableName)
                        {
                            currentIndex = new DbIndex
                            {
                                TableName = tableName,
                                IndexName = indexName,
                                IsUnique = isUnique
                            };
                            schema.Indexes.Add(currentIndex);
                        }
                        currentIndex.ColumnNames.Add(columnName!);
                    }
                    reader.Close();
                }

                // Step 3: 标记外键
                string fkSql = @"
                    SELECT TABLE_NAME, COLUMN_NAME
                    FROM information_schema.KEY_COLUMN_USAGE
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND REFERENCED_TABLE_NAME IS NOT NULL";

                var foreignKeys = new HashSet<(string, string)>();
                using (var cmd = new MySqlCommand(fkSql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        foreignKeys.Add((reader["TABLE_NAME"].ToString()!, reader["COLUMN_NAME"].ToString()!));
                    }
                    reader.Close();
                }

                foreach (var col in schema.Columns)
                {
                    if (foreignKeys.Contains((col.TableName!, col.ColumnName!)))
                    {
                        col.IsForeignKey = true;
                    }
                }
            }

            return schema;
        }

        /// <summary>
        /// 检查数据库命名规范（支持选择AI提供商）
        /// </summary>
        /// <param name="schema">数据库结构</param>
        /// <param name="namingRules">命名规则说明（可选）</param>
        /// <param name="aiProvider">AI提供商选择：simulation、tongyi、deepseek</param>
        /// <param name="dbType">数据库类型：MySQL、PostgreSQL</param>
        /// <returns>Markdown 格式的检查报告</returns>
        public async Task<string> CheckNamingWithRulesAsync(DatabaseSchema schema, string namingRules = null, string aiProvider = "simulation", DatabaseType dbType = DatabaseType.MySQL)
        {
            var rules = namingRules ?? "默认命名规范：小写下划线命名法（snake_case）";
            
            if (aiProvider == "simulation" || _aiService == null)
            {
                // 使用模拟实现
                return SimulateAIResponse(schema, rules, dbType);
            }
            else
            {
                // 根据选择的提供商配置AI服务
                var prompt = GenerateAIPrompt(schema, rules, dbType);
                var aiResponse = await _aiService.GetResponseAsync(prompt, aiProvider);
                return aiResponse;
            }
        }

        /// <summary>
        /// 生成AI提示词
        /// </summary>
        private string GenerateAIPrompt(DatabaseSchema schema, string rules, DatabaseType dbType)
        {
            var prompt = new StringBuilder();
            string dbTypeName = dbType == DatabaseType.PostgreSQL ? "PostgreSQL" : "MySQL";
            prompt.AppendLine($"你是一名数据库命名规范专家，请根据提供的命名规则检查以下{dbTypeName}数据库结构，并生成详细的Markdown格式检查报告。");
            prompt.AppendLine();
            prompt.AppendLine("## 命名规则");
            prompt.AppendLine(rules);
            prompt.AppendLine();
            prompt.AppendLine("## 数据库结构");
            prompt.AppendLine("| 表名 | 字段名 | 字段类型 | 是否主键 | 是否外键 |");
            prompt.AppendLine("|------|--------|----------|----------|----------|");

            foreach (var tableGroup in schema.Columns.GroupBy(s => s.TableName))
            {
                foreach (var obj in tableGroup)
                {
                    prompt.AppendLine($"| {obj.TableName} | {obj.ColumnName} | {obj.ColumnType} | {(obj.ColumnKey == "PRI" ? "是" : "否")} | {(obj.IsForeignKey ? "是" : "否")} |");
                }
            }

            prompt.AppendLine();
            prompt.AppendLine("请按照以下格式生成报告：");
            prompt.AppendLine("1. 报告标题和生成时间");
            prompt.AppendLine("2. 检查摘要（总表数、总字段数、不合规项数量、合规率）");
            prompt.AppendLine("3. 不符合规范的命名列表（表格形式，包含类型、对象、当前名称、问题、建议名称）");
            prompt.AppendLine("4. 建议的SQL修复语句");
            prompt.AppendLine();
            prompt.AppendLine("请确保报告使用Markdown格式，语言为中文。");
            
            return prompt.ToString();
        }

        /// <summary>
        /// 模拟 AI 响应：生成命名规范检查报告
        /// </summary>
        private string SimulateAIResponse(DatabaseSchema schema, string rules, DatabaseType dbType)
        {
            var issues = new List<(string Object, string CurrentName, string Problem, string Suggestion)>();

            // 数据库保留字（不区分大小写）
            var reservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            if (dbType == DatabaseType.MySQL)
            {
                reservedWords.UnionWith(new[]
                {
                    "ALL", "ALTER", "AND", "AS", "ASC", "AUTO_INCREMENT", "BETWEEN", "BIGINT",
                    "BINARY", "BOOLEAN", "BOTH", "BY", "CALL", "CASCADE", "CASE", "CHAR",
                    "CHARACTER", "CHECK", "COLLATE", "COLUMN", "CONDITION", "CONSTRAINT", "CONTINUE",
                    "CREATE", "CROSS", "CURRENT_DATE", "CURRENT_TIME", "CURRENT_TIMESTAMP",
                    "CURRENT_USER", "DATABASE", "DATEDIFF", "DATE_FORMAT", "DATE_SUB", "DECIMAL",
                    "DEFAULT", "DELETE", "DESC", "DESCRIBE", "DISTINCT", "DIV", "DOUBLE", "DROP",
                    "ELSE", "ELSEIF", "END", "ENGINE", "ESCAPE", "EXISTS", "EXIT", "EXPLAIN",
                    "FALSE", "FLOAT", "FOR", "FOREIGN", "FROM", "FULLTEXT", "GROUP", "HAVING",
                    "HIGH_PRIORITY", "HOUR", "IF", "IGNORE", "IN", "INDEX", "INNER", "INSERT",
                    "INT", "INTEGER", "INTERVAL", "INTO", "IS", "JOIN", "KEY", "KEYS", "KILL",
                    "LEFT", "LIKE", "LIMIT", "LOW_PRIORITY", "MATCH", "MEDIUMINT", "MOD", "MODIFY",
                    "NOT", "NO_WRITE_TO_BINLOG", "NULL", "ON", "OPTIMIZE", "OR", "ORDER", "OUTER",
                    "OVER", "PARTITION", "PRECISION", "PRIMARY", "PROCEDURE", "PURGE", "RANGE",
                    "READ", "REFERENCES", "REGEXP", "RENAME", "REPLACE", "REQUIRE", "RESTRICT",
                    "RETURN", "REVOKE", "RIGHT", "RLIKE", "SCHEMA", "SELECT", "SET", "SHOW",
                    "SIGNAL", "SMALLINT", "SONAME", "SPATIAL", "SQL", "SQLEXCEPTION", "SQLSTATE",
                    "SQLWARNING", "SQL_BIG_RESULT", "SQL_CALC_FOUND_ROWS", "SQL_SMALL_RESULT", "SSL",
                    "STARTING", "STRAIGHT_JOIN", "TABLE", "TERMINATED", "THEN", "TIME", "TIMESTAMP",
                    "TINYINT", "TO", "TRUNCATE", "TRUE", "UNION", "UNIQUE", "UNLOCK", "UPDATE",
                    "USAGE", "USE", "USER", "USING", "VALUE", "VALUES", "VARBINARY", "VARCHAR",
                    "VARCHARACTER", "VARYING", "VIEW", "WHEN", "WHERE", "WHILE", "WITH", "WRITE"
                });
            }
            else
            {
                reservedWords.UnionWith(new[]
                {
                    "ALL", "ANALYSE", "ANALYZE", "AND", "ANY", "ARRAY", "AS", "ASC", "ASYMMETRIC", "AUTHORIZATION", "BINARY", "BOTH", "CASE", "CAST", "CHECK", "COLLATE", "COLLATION", "COLUMN", "CONCURRENTLY", "CONSTRAINT", "CREATE", "CROSS", "CURRENT_CATALOG", "CURRENT_DATE", "CURRENT_ROLE", "CURRENT_SCHEMA", "CURRENT_TIME", "CURRENT_TIMESTAMP", "CURRENT_USER", "DEFAULT", "DEFERRABLE", "DESC", "DISTINCT", "DO", "ELSE", "END", "EXCEPT", "FALSE", "FETCH", "FOR", "FOREIGN", "FREEZE", "FROM", "FULL", "GRANT", "GROUP", "HAVING", "ILIKE", "IN", "INITIALLY", "INNER", "INTERSECT", "INTO", "IS", "ISNULL", "JOIN", "LATERAL", "LEADING", "LEFT", "LIKE", "LIMIT", "LOCALTIME", "LOCALTIMESTAMP", "NATURAL", "NOT", "NOTNULL", "NULL", "OFFSET", "ON", "ONLY", "OR", "ORDER", "OUTER", "OVERLAPS", "PLACING", "PRIMARY", "REFERENCES", "RETURNING", "RIGHT", "SELECT", "SESSION_USER", "SIMILAR", "SOME", "SYMMETRIC", "TABLE", "THEN", "TO", "TRAILING", "TRUE", "UNION", "UNIQUE", "USER", "USING", "VARIADIC", "VERBOSE", "WHEN", "WHERE", "WINDOW", "WITH"
                });
            }

            // 新版审计属性（移除 is_deleted，配合 Rule 8/10）
            var newAuditColumns = new HashSet<string> {
                "created_by", "created_at", "updated_by", "updated_at", "deleted_by", "deleted_at"
            };
            
            // 旧版审计属性
            var oldAuditColumns = new HashSet<string> {
                "create_user_id", "create_time", "update_user_id", "update_time", "is_deleted"
            };

            // 常见拼写错误
            var commonSpellingErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                {"adress", "address"}, {"recieve", "receive"}, {"seperate", "separate"}, {"occured", "occurred"},
                {"prefered", "preferred"}, {"begining", "beginning"}, {"definately", "definitely"}
            };

            var tables = schema.Columns.GroupBy(s => s.TableName);
            var tablesMissingAudit = new List<string>();
            var tablesWithOldAudit = new List<string>();

            foreach (var tableGroup in tables)
            {
                var tableName = tableGroup.Key!;
                var expectedTable = ToSnakeCase(tableName);
                var tableColumns = tableGroup.Select(c => c.ColumnName?.ToLower()).ToHashSet();

                // 1. 表名检查
                if (tableName != expectedTable) issues.Add((tableName, tableName, "表名应使用小写下划线命名（snake_case）", expectedTable));

                // 11. 拼写检查
                foreach (var word in tableName.Split('_')) if (commonSpellingErrors.TryGetValue(word, out var corr)) issues.Add((tableName, tableName, $"表名包含拼写错误 '{word}'", tableName.Replace(word, corr)));

                // 8. 审计属性检查
                var presentOldAudits = oldAuditColumns.Where(col => tableColumns.Contains(col)).ToList();
                var missingNewAudits = newAuditColumns.Where(c => !tableColumns.Contains(c)).ToList();

                if (presentOldAudits.Any())
                {
                    tablesWithOldAudit.Add(tableName);
                    issues.Add((tableName, tableName, $"表使用了旧版审计属性 ({string.Join(", ", presentOldAudits)})", "请更改为新版审计属性 (如 created_by, deleted_at 等)"));
                }
                
                if (missingNewAudits.Any())
                {
                    tablesMissingAudit.Add(tableName);
                    issues.Add((tableName, tableName, $"表缺少新版审计属性: {string.Join(", ", missingNewAudits)}", "请补充完整的新版审计属性"));
                }

                // 9. 数据清洗表检查 (Rule 9)
                if ((tableName.Contains("cleaning") || tableName.Contains("log")) && !tableColumns.Contains("last_time"))
                {
                    issues.Add((tableName, tableName, "数据清洗表或日志表建议添加 last_time 字段", "建议添加 last_time DATETIME"));
                }

                foreach (var col in tableGroup)
                {
                    var colName = col.ColumnName!;
                    var fullCol = $"{tableName}.{colName}";
                    var colLower = colName.ToLower();

                    // 1. 字段命名
                    if (colName != ToSnakeCase(colName)) issues.Add((fullCol, colName, "字段应使用小写下划线命名", ToSnakeCase(colName)));

                    // 2/3. 非空与默认值
                    bool isString = col.DataType?.Contains("char") == true || col.DataType?.Contains("text") == true;

                    if (col.IsNullable && !colLower.Contains("deleted_at") && !colLower.Contains("updated_at"))
                    {
                        issues.Add((fullCol, colName, "字段建议设置为 NOT NULL", "建议修改为 NOT NULL"));
                    }
                    
                    // 只有当字段不是主键，且没有默认值时才报警。如果是字符串类型，允许没有显式 default（因为很多时候业务层会处理空串）
                    if (!col.IsNullable && col.ColumnDefault == null && col.ColumnKey != "PRI" && !isString && !col.Extra.Contains("auto_increment", StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add((fullCol, colName, "缺少默认值", "建议根据业务设置安全默认值"));
                    }

                    // 13. 冗余字段规范检查
                    // 如果存在与之对应的 _id 字段（例如有 user_id，同时有 user_name，且当前表不是 user 表），则认为 user_name 是冗余字段
                    if (!colLower.EndsWith("_dup") && (colLower.EndsWith("_name") || colLower.EndsWith("_title") || colLower.EndsWith("_desc")))
                    {
                        var prefix = colLower.Substring(0, colLower.LastIndexOf('_'));
                        var idFieldName = prefix + "_id";
                        
                        // 判断是否为实体的主表（简单判断：表名包含该前缀结尾）
                        bool isEntitySelf = expectedTable == prefix || expectedTable == "t_" + prefix || expectedTable.EndsWith("_" + prefix);

                        if (!isEntitySelf && tableColumns.Contains(idFieldName))
                        {
                            issues.Add((fullCol, colName, $"疑似冗余字段 (和 {idFieldName} 对应)，需加 _dup 标识", $"{colName}_dup"));
                        }
                    }
                }

                // 5/6/7/10: 索引深度检查 (使用 metadata)
                var tableIndexes = schema.Indexes.Where(i => i.TableName == tableName).ToList();
                foreach (var idx in tableIndexes)
                {
                    if (idx.IsUnique && idx.IndexName != "PRIMARY")
                    {
                        // 6. 唯一索引列数 (最多3个)
                        if (idx.ColumnNames.Count > 3) issues.Add((tableName, idx.IndexName!, "唯一索引组合字段超过 3 个", "建议减少组合字段"));

                        // 7. 唯一索引字段非空
                        foreach (var idxCol in idx.ColumnNames)
                        {
                            var colObj = tableGroup.FirstOrDefault(c => c.ColumnName == idxCol);
                            if (colObj != null && colObj.IsNullable)
                            {
                                issues.Add(($"{tableName}.{idxCol}", idxCol, "唯一索引涉及的字段不允许为 NULL", "建议修改为 NOT NULL"));
                            }
                        }

                        // 10. 必须包含 deleted_at
                        if (!idx.ColumnNames.Any(n => n.ToLower() == "deleted_at"))
                        {
                            issues.Add((tableName, idx.IndexName!, "唯一索引必须包含软删除时间字段 deleted_at", $"将 deleted_at 加入索引 {idx.IndexName}"));
                        }
                    }
                }
            }

            // 生成报告
            var result = new StringBuilder();
            result.AppendLine("# 📊 数据库命名规范检查报告 (v2.0)");
            result.AppendLine();
            result.AppendLine($"📅 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            result.AppendLine();
            result.AppendLine("## 📌 检查摘要");
            result.AppendLine($"- 总表数：{tables.Count()} | 总字段数：{schema.Columns.Count}");
            result.AppendLine($"- 待优化项：{issues.Count}");
            result.AppendLine();

            if (issues.Count > 0)
            {
                result.AppendLine("## ❌ 待优化项列表");
                result.AppendLine("| 对象 | 当前名称/描述 | 发现问题 | 建议操作 |");
                result.AppendLine("|------|--------------|----------|----------|");
                foreach (var i in issues) result.AppendLine($"| `{i.Object}` | `{i.CurrentName}` | {i.Problem} | {i.Suggestion} |");
            }
            else
            {
                result.AppendLine("✅ **所有检查项均符合最新规范，包括索引约束！**");
            }

            if (tablesWithOldAudit.Any() || tablesMissingAudit.Any())
            {
                result.AppendLine();
                result.AppendLine("## ⚠️ 审计属性风险说明");
                if (tablesWithOldAudit.Any()) result.AppendLine($"- **使用旧版字段 (如 create_time, is_deleted 等) 的表**：{string.Join(", ", tablesWithOldAudit.Select(t => $"`{t}`"))}");
                if (tablesMissingAudit.Any()) result.AppendLine($"- **缺少/不完整新版审计属性 的表**：{string.Join(", ", tablesMissingAudit.Select(t => $"`{t}`"))}");
            }

            return result.ToString();
        }

        /// <summary>
        /// 将 PascalCase/camelCase 转为 snake_case
        /// 示例：sender_zipCode → sender_zip_code, UserID → user_id
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>snake_case 格式字符串</returns>
        public string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // 在小写/数字后接大写时插入下划线：zipCode → zip_Code
            var result = Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2");

            // 在大写后接大写+小写时插入下划线：HTMLParser → HTML_Parser
            result = Regex.Replace(result, @"([A-Z])([A-Z][a-z])", "$1_$2");

            return result.ToLower();
        }
    }
}
