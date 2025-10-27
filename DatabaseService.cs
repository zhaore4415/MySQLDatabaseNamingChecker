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

            // 新版审计属性
            var newAuditColumns = new HashSet<string> {
                "created_by", "created_at", "updated_by", "updated_at", "deleted_by", "deleted_at", "is_deleted"
            };
            
            // 旧版审计属性
            var oldAuditColumns = new HashSet<string> {
                "create_user_id", "create_time", "update_user_id", "update_time", "is_deleted"
            };

            // 常见拼写错误词汇表
             var commonSpellingErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                 {"adress", "address"}, {"recieve", "receive"}, {"seperate", "separate"}, {"occured", "occurred"},
                 {"prefered", "preferred"}, {"begining", "beginning"}, {"definately", "definitely"},
                 {"dissapear", "disappear"}, {"embarass", "embarrass"}, {"goverment", "government"},
                 {"neccessary", "necessary"}, {"suport", "support"}, {"thier", "their"}, 
                 {"acount", "account"}, {"conect", "connect"}
             };

            var tables = schema.GroupBy(s => s.TableName);
            var tablesMissingAuditColumns = new List<string>();
            var tablesWithOldAuditColumns = new List<string>();

            foreach (var tableGroup in tables)
            {
                var tableName = tableGroup.Key;
                if (string.IsNullOrEmpty(tableName)) continue;
                
                var expectedTable = ToSnakeCase(tableName);
                var tableColumns = tableGroup.Select(c => c.ColumnName?.ToLower()).Where(c => c != null).ToHashSet();
                var hasOldAudit = oldAuditColumns.Any(col => tableColumns.Contains(col));
                var hasNewAudit = newAuditColumns.All(col => tableColumns.Contains(col));
                
                // 1. ✅ 表名检查（统一使用 snake_case）
                if (tableName != expectedTable)
                {
                    issues.Add((tableName, tableName, "表名应使用小写下划线命名（snake_case）", expectedTable));
                }

                // 11. ✅ 表名拼写检查
                foreach (var word in tableName.Split('_'))
                {
                    if (commonSpellingErrors.TryGetValue(word, out var correction))
                    {
                        issues.Add((tableName, tableName, $"表名包含拼写错误的单词 '{word}'", 
                            tableName.Replace(word, correction, StringComparison.OrdinalIgnoreCase)));
                        break;
                    }
                }

                if (reservedWords.Contains(tableName.ToUpper()))
                {
                    issues.Add((tableName, tableName, "表名使用了MySQL保留字", $"tbl_{expectedTable}"));
                }

                // 12. ✅ 检查审计属性
                if (hasOldAudit)
                {
                    tablesWithOldAuditColumns.Add(tableName);
                    issues.Add((tableName, tableName, "表使用了旧版审计属性，应使用新版审计属性", "需要更新为新版审计属性"));
                }
                
                if (!hasNewAudit)
                {
                    tablesMissingAuditColumns.Add(tableName);
                    var missingColumns = newAuditColumns.Where(col => !tableColumns.Contains(col)).ToList();
                    issues.Add((tableName, tableName, $"表缺少必要的新版审计属性: {string.Join(", ", missingColumns)}", "需要添加所有新版审计属性"));
                }

                foreach (var obj in tableGroup)
                {
                    if (string.IsNullOrEmpty(obj.ColumnName)) continue;
                    
                    var fullColName = $"{obj.TableName}.{obj.ColumnName}";
                    var expectedCol = ToSnakeCase(obj.ColumnName);
                    var colNameLower = obj.ColumnName.ToLower();

                    // 2. ✅ 字段名检查
                    if (obj.ColumnName != expectedCol)
                    {
                        issues.Add((fullColName, obj.ColumnName, "字段名应使用小写下划线命名（snake_case）", expectedCol));
                    }

                    // 11. ✅ 字段名拼写检查
                    foreach (var word in obj.ColumnName.Split('_'))
                    {
                        if (commonSpellingErrors.TryGetValue(word, out var correction))
                        {
                            issues.Add((fullColName, obj.ColumnName, $"字段名包含拼写错误的单词 '{word}'", 
                                obj.ColumnName.Replace(word, correction, StringComparison.OrdinalIgnoreCase)));
                            break;
                        }
                    }

                    // 保留字检查
                    if (reservedWords.Contains(obj.ColumnName.ToUpper()))
                    {
                        issues.Add((fullColName, obj.ColumnName, "字段名使用了MySQL保留字", $"col_{expectedCol}"));
                    }

                    // 主键检查
                    if (obj.ColumnKey == "PRI" && obj.ColumnName != "id")
                    {
                        issues.Add((fullColName, obj.ColumnName, "主键字段建议命名为 'id'", "id"));
                    }

                    // 外键检查
                    if (obj.IsForeignKey && !colNameLower.EndsWith("_id"))
                    {
                        var suggested = expectedCol.EndsWith("_id") ? expectedCol : expectedCol + "_id";
                        issues.Add((fullColName, obj.ColumnName, "外键字段应以 '_id' 结尾", suggested));
                    }

                    // 3. ✅ 字段非空检查
                    if (obj.IsNullable && !colNameLower.EndsWith("_at") && !newAuditColumns.Contains(colNameLower))
                    {
                        issues.Add((fullColName, obj.ColumnName, "字段应设置为非空（NOT NULL）", "建议修改为 NOT NULL"));
                    }

                    // 4. ✅ 默认值检查 - 跳过主键字段（主键通常是自增长的）
                     if (!obj.IsNullable && string.IsNullOrEmpty(obj.ColumnDefault) && obj.ColumnKey != "PRI")
                     {
                        string suggestedDefault = "";
                        string dataType = obj.DataType?.ToLower() ?? "";
                        
                        if (colNameLower.EndsWith("_id"))
                        {
                            suggestedDefault = "0";
                        }
                        else if (dataType.Contains("varchar") || dataType.Contains("text"))
                        {
                            suggestedDefault = "''";
                        }
                        else if (dataType.Contains("date") || dataType.Contains("time") || dataType.Contains("timestamp"))
                        {
                            suggestedDefault = "'1970-01-01 00:00:00'";
                        }
                        else if (dataType.Contains("int") || dataType.Contains("decimal") || dataType.Contains("float") || dataType.Contains("double"))
                        {
                            suggestedDefault = "0";
                        }
                        else if (dataType.Contains("boolean") || dataType.Contains("bit"))
                        {
                            suggestedDefault = "0";
                        }
                        
                        if (!string.IsNullOrEmpty(suggestedDefault))
                        {
                            issues.Add((fullColName, obj.ColumnName, $"字段缺少合适的默认值", $"建议设置默认值为 {suggestedDefault}"));
                        }
                    }

                    // 5. ✅ 约束检查（暂时不实现，需要获取约束信息）
                    // 6. ✅ 唯一索引检查（暂时不实现，需要获取索引信息）
                    // 7. ✅ 唯一索引字段非空检查（暂时不实现）
                    // 8. ✅ 已在表级检查审计属性
                    // 9. ✅ 数据清洗表检查（需要额外标记哪些是数据清洗表）
                }
            }

            // 查找缺少特定新版审计属性的表（如deleted_by、deleted_at）
            var tablesMissingSpecificAuditColumns = new Dictionary<string, List<string>>();
            foreach (var tableGroup in tables)
            {
                var tableName = tableGroup.Key;
                var tableColumns = tableGroup.Select(c => c.ColumnName?.ToLower()).Where(c => c != null).ToHashSet();
                
                // 检查新版审计属性中的重要字段
                var specificMissing = new List<string>();
                if (!tableColumns.Contains("deleted_by")) specificMissing.Add("deleted_by");
                if (!tableColumns.Contains("deleted_at")) specificMissing.Add("deleted_at");
                
                if (specificMissing.Any())
                {
                    tablesMissingSpecificAuditColumns[tableName] = specificMissing;
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
            result.AppendLine($"- **缺少审计属性的表**：{tablesMissingAuditColumns.Count}");
            result.AppendLine($"- **使用旧版审计属性的表**：{tablesWithOldAuditColumns.Count}");
            result.AppendLine($"- **缺少特定新版审计属性的表**：{tablesMissingSpecificAuditColumns.Count}");
            result.AppendLine();

            if (issues.Count == 0)
            {
                result.AppendLine("✅ **所有命名均符合规范，恭喜！**");
            }
            else
            {
                // 按对象类型分组显示问题
                result.AppendLine("## ❌ 不符合规范的命名");
                result.AppendLine();
                
                // 表级问题
                var tableIssues = issues.Where(i => !i.Object.Contains(".")).ToList();
                if (tableIssues.Count > 0)
                {
                    result.AppendLine("### 📋 表级问题");
                    result.AppendLine();
                    result.AppendLine("| 对象 | 当前名称 | 问题 | 建议名称 |");
                    result.AppendLine("|------|----------|------|----------|");
                    
                    foreach (var issue in tableIssues)
                    {
                        result.AppendLine($"| `{issue.Object}` | `{issue.CurrentName}` | {issue.Problem} | `{issue.Suggestion}` |");
                    }
                    result.AppendLine();
                }
                
                // 字段级问题
                var columnIssues = issues.Where(i => i.Object.Contains(".")).ToList();
                if (columnIssues.Count > 0)
                {
                    result.AppendLine("### 📝 字段级问题");
                    result.AppendLine();
                    result.AppendLine("| 对象 | 当前名称 | 问题 | 建议名称/修改 |");
                    result.AppendLine("|------|----------|------|---------------|");
                    
                    foreach (var issue in columnIssues)
                    {
                        result.AppendLine($"| `{issue.Object}` | `{issue.CurrentName}` | {issue.Problem} | {issue.Suggestion} |");
                    }
                    result.AppendLine();
                }

                // 特别提醒：审计属性问题
                if (tablesWithOldAuditColumns.Count > 0 || tablesMissingSpecificAuditColumns.Count > 0)
                {
                    result.AppendLine("## ⚠️ 审计属性警告");
                    result.AppendLine();
                    
                    if (tablesWithOldAuditColumns.Count > 0)
                    {
                        result.AppendLine("### 使用旧版审计属性的表（需要更新）：");
                        result.AppendLine();
                        foreach (var tableName in tablesWithOldAuditColumns)
                        {
                            result.AppendLine($"- `{tableName}`");
                        }
                        result.AppendLine();
                    }
                    
                    if (tablesMissingSpecificAuditColumns.Count > 0)
                    {
                        result.AppendLine("### 缺少特定新版审计属性的表（如deleted_by、deleted_at）：");
                        result.AppendLine();
                        foreach (var kvp in tablesMissingSpecificAuditColumns)
                        {
                            result.AppendLine($"- `{kvp.Key}`：缺少 {string.Join(", ", kvp.Value)}");
                        }
                        result.AppendLine();
                    }
                    
                    result.AppendLine("### 新版审计属性应包含：");
                    result.AppendLine();
                    result.AppendLine("```sql");
                    result.AppendLine("-- 新版审计属性字段");
                    result.AppendLine("created_by VARCHAR(50) NOT NULL COMMENT '创建人',");
                    result.AppendLine("created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',");
                    result.AppendLine("updated_by VARCHAR(50) NOT NULL COMMENT '更新人',");
                    result.AppendLine("updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',");
                    result.AppendLine("deleted_by VARCHAR(50) NULL COMMENT '删除人',");
                    result.AppendLine("deleted_at DATETIME NULL COMMENT '删除时间',");
                    result.AppendLine("is_deleted TINYINT NOT NULL DEFAULT 0 COMMENT '软删除标记'");
                    result.AppendLine("```");
                    result.AppendLine();
                }

                result.AppendLine("## 🔧 建议的 SQL 修复语句");
                result.AppendLine("```sql");
                
                // 生成表重命名语句
                foreach (var issue in tableIssues.Where(i => i.Problem.Contains("表名应使用"))) {
                    result.AppendLine($"ALTER TABLE `{issue.CurrentName}` RENAME TO `{issue.Suggestion}`;");
                }
                result.AppendLine();
                
                // 生成字段修改语句
                foreach (var issue in columnIssues)
                {
                    if (issue.Object.Contains("."))
                    {
                        var parts = issue.Object.Split('.');
                        var column = schema.Find(c => c.TableName == parts[0] && c.ColumnName == parts[1]);
                        if (column != null)
                        {
                            if (issue.Problem.Contains("字段名应使用"))
                            {
                                result.AppendLine($"ALTER TABLE `{parts[0]}` CHANGE COLUMN `{parts[1]}` `{issue.Suggestion}` {column.ColumnType};");
                            }
                            else if (issue.Problem.Contains("字段应设置为非空"))
                            {
                                result.AppendLine($"ALTER TABLE `{parts[0]}` MODIFY COLUMN `{parts[1]}` {column.ColumnType} NOT NULL;");
                            }
                            else if (issue.Problem.Contains("字段缺少合适的默认值"))
                            {
                                var defaultValue = issue.Suggestion.Split("为 ")[1];
                                result.AppendLine($"ALTER TABLE `{parts[0]}` MODIFY COLUMN `{parts[1]}` {column.ColumnType} DEFAULT {defaultValue};");
                            }
                        }
                    }
                }
                
                // 添加缺失的审计属性示例
                if (tablesMissingAuditColumns.Count > 0)
                {
                    result.AppendLine();
                    result.AppendLine("-- 为缺失审计属性的表添加新版审计字段示例");
                    foreach (var tableName in tablesMissingAuditColumns.Take(3))
                    {  // 只显示前3个示例
                        result.AppendLine($"ALTER TABLE `{tableName}`");
                        result.AppendLine("ADD COLUMN created_by VARCHAR(50) NOT NULL DEFAULT '',");
                        result.AppendLine("ADD COLUMN created_at DATETIME NOT NULL DEFAULT '1970-01-01 00:00:00',");
                        result.AppendLine("ADD COLUMN updated_by VARCHAR(50) NOT NULL DEFAULT '',");
                        result.AppendLine("ADD COLUMN updated_at DATETIME NOT NULL DEFAULT '1970-01-01 00:00:00' ON UPDATE CURRENT_TIMESTAMP,");
                        result.AppendLine("ADD COLUMN deleted_by VARCHAR(50) NULL,");
                        result.AppendLine("ADD COLUMN deleted_at DATETIME NULL,");
                        result.AppendLine("ADD COLUMN is_deleted TINYINT NOT NULL DEFAULT 0;");
                        result.AppendLine();
                    }
                }
                
                // 添加唯一索引规范的注释说明
                result.AppendLine();
                result.AppendLine("-- 唯一索引规范提醒");
                result.AppendLine("-- 1. 唯一索引最好只包含一个列");
                result.AppendLine("-- 2. 最多允许三个字段组成唯一索引");
                result.AppendLine("-- 3. 唯一索引的字段不允许为null");
                result.AppendLine("-- 4. 唯一索引需要包含软删除字段(is_deleted)，如：");
                result.AppendLine("-- CREATE UNIQUE INDEX uk_field_name ON table_name(field_name, is_deleted);");
                
                // 数据清洗表的注释说明
                result.AppendLine();
                result.AppendLine("-- 数据清洗表建议");
                result.AppendLine("-- 需要数据清洗的表，建议添加last_time字段，不会与update_time冲突");
                result.AppendLine("-- ALTER TABLE data_cleaning_table ADD COLUMN last_time DATETIME NOT NULL DEFAULT '1970-01-01 00:00:00';");
                
                result.AppendLine("```");
                result.AppendLine();
                result.AppendLine("## 📋 命名规范要求总结");
                result.AppendLine("1. 表名和列名小写加下划线，如: subject_category表，parent_id列");
                result.AppendLine("2. 所有字段不可为空，如parent_id等id类默认值为0，字符串类默认值为空字符串('')，日期时间类默认值为1970-1");
                result.AppendLine("3. 其他类型视业务场景取一个安全值作为默认值，如余额为null时默认值是0");
                result.AppendLine("4. 所有表需要完整审计属性，即创建人、创建时间、更新人、更新时间、软删除");
                result.AppendLine("5. 除主键和唯一索引外，不可添加其他任何约束，唯一索引尽量只添加一组");
                result.AppendLine("6. 唯一索引最好只包括一个列，如单列实在无法保证唯一性，最多只允许三个字段");
                result.AppendLine("7. 唯一索引的字段不允许为null，null值会加大唯一性检查的复杂度，会进一步降低性能");
                result.AppendLine("8. 新版审计属性：created_by, created_at, updated_by, updated_at, deleted_by, deleted_at, is_deleted");
                result.AppendLine("9. 需要数据清洗的表，添加一个last_time字段，不会与update_time冲突");
                result.AppendLine("10. 唯一索引需要包含软删除字段(is_deleted)，防止新增冲突");
                result.AppendLine("11. 表名和字段名的单词不应有拼写错误");
                result.AppendLine("12. 所有表应包含完整的新版审计属性，特别是deleted_by、deleted_at");
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
