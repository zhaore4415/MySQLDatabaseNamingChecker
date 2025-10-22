using MySql.Data.MySqlClient;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

namespace DBCheckAI
{
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
        /// 从 MySQL 数据库提取表和字段信息（含完整列类型）
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <returns>数据库对象列表</returns>
        public async Task<List<DbObject>> GetMySQLDatabaseSchemaAsync(string connectionString)
        {
            var result = new List<DbObject>();

            using (var conn = new MySqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // Step 1: 获取所有字段信息（包含完整 COLUMN_TYPE）
                string columnsSql = @"
                    SELECT 
                        TABLE_NAME,
                        COLUMN_NAME,
                        DATA_TYPE,
                        COLUMN_TYPE,
                        IS_NULLABLE,
                        COLUMN_KEY,
                        COLUMN_DEFAULT,
                        EXTRA
                    FROM information_schema.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                    ORDER BY TABLE_NAME, ORDINAL_POSITION";

                // Step 2: 获取外键字段（排除主键）
                var foreignKeys = new HashSet<(string TableName, string ColumnName)>();
                string fkSql = @"
                    SELECT 
                        TABLE_NAME,
                        COLUMN_NAME
                    FROM information_schema.KEY_COLUMN_USAGE
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND REFERENCED_TABLE_NAME IS NOT NULL
                      AND CONSTRAINT_NAME != 'PRIMARY'"; // 排除主键

                using (var fkCmd = new MySqlCommand(fkSql, conn))
                using (var fkReader = await fkCmd.ExecuteReaderAsync())
                {
                    while (await fkReader.ReadAsync())
                    {
                        var tableName = fkReader["TABLE_NAME"].ToString();
                        var columnName = fkReader["COLUMN_NAME"].ToString();
                        if (tableName != null && columnName != null)
                        {
                            foreignKeys.Add((tableName, columnName));
                        }
                    }
                    fkReader.Close(); // 关闭后执行下一个查询
                }

                // 执行字段查询
                using (var cmd = new MySqlCommand(columnsSql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var tableName = reader["TABLE_NAME"].ToString();
                        var columnName = reader["COLUMN_NAME"].ToString();

                        result.Add(new DbObject
                        {
                            TableName = tableName,
                            ColumnName = columnName,
                            DataType = reader["DATA_TYPE"].ToString(),
                            ColumnType = reader["COLUMN_TYPE"]?.ToString() ?? reader["DATA_TYPE"].ToString(), // fallback
                            IsNullable = reader["IS_NULLABLE"].ToString() == "YES",
                            ColumnKey = reader["COLUMN_KEY"].ToString(),
                            ColumnDefault = reader["COLUMN_DEFAULT"]?.ToString(),
                            Extra = reader["EXTRA"]?.ToString(),
                            IsForeignKey = tableName != null && columnName != null && foreignKeys.Contains((tableName, columnName))
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 检查数据库命名规范（支持选择AI提供商）
        /// </summary>
        /// <param name="schema">数据库结构列表</param>
        /// <param name="namingRules">命名规则说明（可选）</param>
        /// <param name="aiProvider">AI提供商选择：simulation、tongyi、deepseek</param>
        /// <returns>Markdown 格式的检查报告</returns>
        public async Task<string> CheckNamingWithRulesAsync(List<DbObject> schema, string namingRules = null, string aiProvider = "simulation")
        {
            var rules = namingRules ?? "默认命名规范：小写下划线命名法（snake_case）";
            
            if (aiProvider == "simulation" || _aiService == null)
            {
                // 使用模拟实现
                return SimulateAIResponse(schema, rules);
            }
            else
            {
                // 根据选择的提供商配置AI服务
                var prompt = GenerateAIPrompt(schema, rules);
                var aiResponse = await _aiService.GetResponseAsync(prompt, aiProvider);
                return aiResponse;
            }
        }

        /// <summary>
        /// 生成AI提示词
        /// </summary>
        private string GenerateAIPrompt(List<DbObject> schema, string rules)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("你是一名数据库命名规范专家，请根据提供的命名规则检查以下MySQL数据库结构，并生成详细的Markdown格式检查报告。");
            prompt.AppendLine();
            prompt.AppendLine("## 命名规则");
            prompt.AppendLine(rules);
            prompt.AppendLine();
            prompt.AppendLine("## 数据库结构");
            prompt.AppendLine("| 表名 | 字段名 | 字段类型 | 是否主键 | 是否外键 |");
            prompt.AppendLine("|------|--------|----------|----------|----------|");

            foreach (var tableGroup in schema.GroupBy(s => s.TableName))
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
        private string SimulateAIResponse(List<DbObject> schema, string rules)
        {
            var issues = new List<(string Object, string CurrentName, string Problem, string Suggestion)>();

            // MySQL 保留字（不区分大小写）
            var reservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
            };

            var tables = schema.GroupBy(s => s.TableName);

            foreach (var tableGroup in tables)
            {
                var tableName = tableGroup.Key;
                var expectedTable = ToSnakeCase(tableName);

                // ✅ 表名检查（统一使用 snake_case）
                if (tableName != expectedTable)
                {
                    issues.Add((tableName, tableName, "表名应使用小写下划线命名（snake_case）", expectedTable));
                }

                if (reservedWords.Contains(tableName.ToUpper()))
                {
                    issues.Add((tableName, tableName, "表名使用了MySQL保留字", $"tbl_{expectedTable}"));
                }

                foreach (var obj in tableGroup)
                {
                    var fullColName = $"{obj.TableName}.{obj.ColumnName}";
                    var expectedCol = ToSnakeCase(obj.ColumnName);

                    // ✅ 字段名检查
                    if (obj.ColumnName != expectedCol)
                    {
                        issues.Add((fullColName, obj.ColumnName, "字段名应使用小写下划线命名（snake_case）", expectedCol));
                    }

                    // ✅ 保留字检查
                    if (reservedWords.Contains(obj.ColumnName.ToUpper()))
                    {
                        issues.Add((fullColName, obj.ColumnName, "字段名使用了MySQL保留字", $"col_{expectedCol}"));
                    }

                    // ✅ 主键检查
                    if (obj.ColumnKey == "PRI" && obj.ColumnName != "id")
                    {
                        issues.Add((fullColName, obj.ColumnName, "主键字段建议命名为 'id'", "id"));
                    }

                    // ✅ 外键检查
                    if (obj.IsForeignKey && !obj.ColumnName.EndsWith("_id"))
                    {
                        var suggested = expectedCol.EndsWith("_id") ? expectedCol : expectedCol + "_id";
                        issues.Add((fullColName, obj.ColumnName, "外键字段应以 '_id' 结尾", suggested));
                    }
                }
            }

            // 生成 Markdown 报告
            var result = new StringBuilder();
            result.AppendLine("# 📊 MySQL数据库命名规范检查报告");
            result.AppendLine();
            result.AppendLine($"📅 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            result.AppendLine();
            result.AppendLine("## 📌 检查摘要");
            result.AppendLine();
            result.AppendLine($"- **总表数**：{tables.Count()}");
            result.AppendLine($"- **总字段数**：{schema.Count}");
            result.AppendLine($"- **不合规项**：{issues.Count}");
            result.AppendLine($"- **合规率**：{(schema.Count > 0 ? (int)((double)(schema.Count - issues.Count) / schema.Count * 100) : 100)}%");
            result.AppendLine();

            if (issues.Count == 0)
            {
                result.AppendLine("✅ **所有命名均符合规范，恭喜！**");
            }
            else
            {
                result.AppendLine("## ❌ 不符合规范的命名");
                result.AppendLine();
                result.AppendLine("| 类型 | 对象 | 当前名称 | 问题 | 建议名称 |");
                result.AppendLine("|------|------|----------|------|----------|");

                foreach (var issue in issues)
                {
                    var type = issue.Object.Contains(".") ? "字段" : "表名";
                    result.AppendLine($"| {type} | `{issue.Object}` | `{issue.CurrentName}` | {issue.Problem} | `{issue.Suggestion}` |");
                }

                result.AppendLine();
                result.AppendLine("## 🔧 建议的 SQL 修复语句");
                result.AppendLine("```sql");
                foreach (var issue in issues)
                {
                    if (issue.Object.Contains("."))
                    {
                        var parts = issue.Object.Split('.');
                        var column = schema.Find(c => c.TableName == parts[0] && c.ColumnName == parts[1]);
                        if (column != null)
                        {
                            result.AppendLine($"ALTER TABLE `{parts[0]}` CHANGE COLUMN `{parts[1]}` `{issue.Suggestion}` {column.ColumnType};");
                        }
                    }
                    else
                    {
                        result.AppendLine($"ALTER TABLE `{issue.CurrentName}` RENAME TO `{issue.Suggestion}`;");
                    }
                }
                result.AppendLine("```");
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
