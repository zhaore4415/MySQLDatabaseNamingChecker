using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Markdig;

namespace DBCheckAI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 添加Razor Pages服务
            builder.Services.AddRazorPages();
            // 注册数据库服务
            builder.Services.AddSingleton<DatabaseService>();

            var app = builder.Build();

            // 配置HTTP请求管道
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }

    // 数据库服务类
    public class DatabaseService
    {
        // 提取MySQL数据库表和字段信息
        public async Task<List<DbObject>> GetMySQLDatabaseSchemaAsync(string connectionString)
        {
            var result = new List<DbObject>();

            using (var conn = new MySqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // 查询所有表和字段信息
                string sql = @"
                    SELECT 
                        TABLE_NAME,
                        COLUMN_NAME,
                        DATA_TYPE,
                        IS_NULLABLE,
                        COLUMN_KEY,
                        COLUMN_DEFAULT,
                        EXTRA
                    FROM information_schema.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                    ORDER BY TABLE_NAME, ORDINAL_POSITION";

                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new DbObject
                        {
                            TableName = reader["TABLE_NAME"].ToString(),
                            ColumnName = reader["COLUMN_NAME"].ToString(),
                            DataType = reader["DATA_TYPE"].ToString(),
                            IsNullable = reader["IS_NULLABLE"].ToString() == "YES",
                            ColumnKey = reader["COLUMN_KEY"].ToString(),
                            ColumnDefault = reader["COLUMN_DEFAULT"]?.ToString(),
                            Extra = reader["EXTRA"]?.ToString(),
                            ConstraintType = string.Empty // 初始化约束类型
                        });
                    }
                }
            }

            return result;
        }

        // 检查命名规范
        public async Task<string> CheckNamingWithRulesAsync(List<DbObject> schema, string namingRules)
        {
            // 构造输入内容
            var sb = new StringBuilder();
            sb.AppendLine("请根据以下命名规范，检查MySQL数据库对象是否合规。");
            sb.AppendLine(namingRules);
            sb.AppendLine();
            sb.AppendLine("## 数据库对象列表");
            sb.AppendLine("格式：表名.字段名 (数据类型)");

            // 按表分组显示
            var grouped = new Dictionary<string, List<(string Column, string Type)>>();
            foreach (var obj in schema)
            {
                if (!grouped.ContainsKey(obj.TableName))
                    grouped[obj.TableName] = new List<(string, string)>();
                grouped[obj.TableName].Add((obj.ColumnName, obj.DataType));
            }

            foreach (var kvp in grouped)
            {
                sb.AppendLine($"表: {kvp.Key}");
                foreach (var columnInfo in kvp.Value)
                {
                    sb.AppendLine($"  - {kvp.Key}.{columnInfo.Column} ({columnInfo.Type})");
                }
                sb.AppendLine();
            }

            // 调用模拟的检查函数
            return SimulateAIResponse(schema, namingRules);
        }

        // 模拟AI响应（输出为 Markdown 格式）
        public string SimulateAIResponse(List<DbObject> schema, string rules)
        {
            var issues = new List<(string Object, string CurrentName, string Problem, string Suggestion)>();

            // MySQL保留字列表
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

            foreach (var obj in schema)
            {
                // 检查表名：应为小写下划线
                if (obj.TableName != obj.TableName.ToLower())
                {
                    var suggested = ToSnakeCase(obj.TableName);
                    issues.Add((obj.TableName, obj.TableName, "表名应使用小写命名", suggested));
                }

                // 检查表名是否为保留字
                if (reservedWords.Contains(obj.TableName))
                {
                    issues.Add((obj.TableName, obj.TableName, "表名使用了MySQL保留字", $"tbl_{obj.TableName}"));
                }

                // 检查字段名
                if (obj.ColumnName != obj.ColumnName.ToLower())
                {
                    var suggested = ToSnakeCase(obj.ColumnName);
                    issues.Add((obj.TableName + "." + obj.ColumnName, obj.ColumnName, "字段名应使用小写命名", suggested));
                }

                // 检查字段名是否为保留字
                if (reservedWords.Contains(obj.ColumnName))
                {
                    issues.Add((obj.TableName + "." + obj.ColumnName, obj.ColumnName, "字段名使用了MySQL保留字", $"col_{obj.ColumnName}"));
                }

                // 检查主键命名
                if (obj.ColumnKey == "PRI" && obj.ColumnName != "id")
                {
                    issues.Add((obj.TableName + "." + obj.ColumnName, obj.ColumnName, "主键字段应命名为'id'", "id"));
                }

                // 检查外键命名（简化判断，实际应基于外键约束）
                if (obj.ColumnName.Contains("id") && !obj.ColumnName.EndsWith("_id"))
                {
                    // 排除主键
                    if (obj.ColumnKey != "PRI")
                    {
                        var suggested = ToSnakeCase(obj.ColumnName) + "_id";
                        issues.Add((obj.TableName + "." + obj.ColumnName, obj.ColumnName, "外键字段应以 '_id' 结尾", suggested));
                    }
                }
            }

            var result = new StringBuilder();

            // Markdown 标题
            result.AppendLine("# 📊 MySQL数据库命名规范检查报告");
            result.AppendLine();
            result.AppendLine($"📅 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            result.AppendLine();
            result.AppendLine("## 📌 检查摘要");
            result.AppendLine();
            result.AppendLine($"- **总表数**：{schema.Select(s => s.TableName).Distinct().Count()}");
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
                        result.AppendLine($"ALTER TABLE `{parts[0]}` CHANGE COLUMN `{parts[1]}` `{issue.Suggestion}` {GetColumnDataType(schema, parts[0], parts[1])};");
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

        // 获取字段的数据类型
        private string GetColumnDataType(List<DbObject> schema, string tableName, string columnName)
        {
            var column = schema.FirstOrDefault(c => c.TableName == tableName && c.ColumnName == columnName);
            if (column != null)
            {
                // 简化的数据类型返回，实际应用中可能需要更复杂的处理
                return column.DataType;
            }
            return "VARCHAR(255)";
        }

        /// <summary>
        /// 将 PascalCase/camelCase 转为 snake_case
        /// </summary>
        public string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // 在小写字母后接大写字母的地方插入下划线
            var result = Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2");
            // 全部转为小写
            return result.ToLower();
        }
    }

    // 数据模型
    public class DbObject
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public string ColumnKey { get; set; }
        public string ColumnDefault { get; set; }
        public string Extra { get; set; }
        public string ConstraintType { get; set; }
    }
}

