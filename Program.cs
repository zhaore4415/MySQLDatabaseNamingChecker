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
using System.Reflection.PortableExecutable;

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

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }

    //// 数据库服务类
    //public class DatabaseService
    //{
    //    // 提取MySQL数据库表和字段信息
    //    public async Task<List<DbObject>> GetMySQLDatabaseSchemaAsync(string connectionString)
    //    {
    //        var result = new List<DbObject>();

    //        using (var conn = new MySqlConnection(connectionString))
    //        {
    //            await conn.OpenAsync();

    //            // Step 1: 获取所有字段信息
    //            string columnsSql = @"
    //        SELECT 
    //            TABLE_NAME,
    //            COLUMN_NAME,
    //            DATA_TYPE,
    //            IS_NULLABLE,
    //            COLUMN_KEY,
    //            COLUMN_DEFAULT,
    //            EXTRA
    //        FROM information_schema.COLUMNS
    //        WHERE TABLE_SCHEMA = DATABASE()
    //        ORDER BY TABLE_NAME, ORDINAL_POSITION";

    //            // Step 2: 获取外键信息
    //            var foreignKeys = new HashSet<(string TableName, string ColumnName)>();
    //            string fkSql = @"
    //        SELECT 
    //            TABLE_NAME,
    //            COLUMN_NAME
    //        FROM information_schema.KEY_COLUMN_USAGE
    //        WHERE TABLE_SCHEMA = DATABASE()
    //          AND REFERENCED_TABLE_NAME IS NOT NULL"; // 有引用表即为外键

    //            using (var fkCmd = new MySqlCommand(fkSql, conn))
    //            using (var fkReader = await fkCmd.ExecuteReaderAsync())
    //            {
    //                while (await fkReader.ReadAsync())
    //                {
    //                    var tableName = fkReader["TABLE_NAME"].ToString();
    //                    var columnName = fkReader["COLUMN_NAME"].ToString();
    //                    foreignKeys.Add((tableName, columnName));
    //                }
    //                // 关闭 reader 后再执行下一个查询
    //                fkReader.Close();
    //            }

    //            // 执行字段查询
    //            using (var cmd = new MySqlCommand(columnsSql, conn))
    //            using (var reader = await cmd.ExecuteReaderAsync())
    //            {
    //                while (await reader.ReadAsync())
    //                {
    //                    var tableName = reader["TABLE_NAME"].ToString();
    //                    var columnName = reader["COLUMN_NAME"].ToString();

    //                    result.Add(new DbObject
    //                    {
    //                        TableName = tableName,
    //                        ColumnName = columnName,
    //                        DataType = reader["DATA_TYPE"].ToString(),
    //                        IsNullable = reader["IS_NULLABLE"].ToString() == "YES",
    //                        ColumnKey = reader["COLUMN_KEY"].ToString(),
    //                        ColumnDefault = reader["COLUMN_DEFAULT"]?.ToString(),
    //                        Extra = reader["EXTRA"]?.ToString(),
    //                        ConstraintType = string.Empty,
    //                        IsForeignKey = foreignKeys.Contains((tableName, columnName)) // ✅ 设置外键标志
    //                    });
    //                }
    //            }
    //        }

    //        return result;
    //    }

    //    // 检查命名规范
    //    public async Task<string> CheckNamingWithRulesAsync(List<DbObject> schema, string namingRules)
    //    {
    //        // 构造输入内容
    //        var sb = new StringBuilder();
    //        sb.AppendLine("请根据以下命名规范，检查MySQL数据库对象是否合规。");
    //        sb.AppendLine(namingRules);
    //        sb.AppendLine();
    //        sb.AppendLine("## 数据库对象列表");
    //        sb.AppendLine("格式：表名.字段名 (数据类型)");

    //        // 按表分组显示
    //        var grouped = new Dictionary<string, List<(string Column, string Type)>>();
    //        foreach (var obj in schema)
    //        {
    //            if (!grouped.ContainsKey(obj.TableName))
    //                grouped[obj.TableName] = new List<(string, string)>();
    //            grouped[obj.TableName].Add((obj.ColumnName, obj.DataType));
    //        }

    //        foreach (var kvp in grouped)
    //        {
    //            sb.AppendLine($"表: {kvp.Key}");
    //            foreach (var columnInfo in kvp.Value)
    //            {
    //                sb.AppendLine($"  - {kvp.Key}.{columnInfo.Column} ({columnInfo.Type})");
    //            }
    //            sb.AppendLine();
    //        }

    //        // 调用模拟的检查函数
    //        return SimulateAIResponse(schema, namingRules);
    //    }

    //    // 模拟AI响应（输出为 Markdown 格式）
    //    public string SimulateAIResponse(List<DbObject> schema, string rules)
    //    {
    //        var issues = new List<(string Object, string CurrentName, string Problem, string Suggestion)>();

    //        // MySQL保留字列表
    //        var reservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    //        {
    //            "ALL", "ALTER", "AND", "AS", "ASC", "AUTO_INCREMENT", "BETWEEN", "BIGINT",
    //            "BINARY", "BOOLEAN", "BOTH", "BY", "CALL", "CASCADE", "CASE", "CHAR",
    //            "CHARACTER", "CHECK", "COLLATE", "COLUMN", "CONDITION", "CONSTRAINT", "CONTINUE",
    //            "CREATE", "CROSS", "CURRENT_DATE", "CURRENT_TIME", "CURRENT_TIMESTAMP",
    //            "CURRENT_USER", "DATABASE", "DATEDIFF", "DATE_FORMAT", "DATE_SUB", "DECIMAL",
    //            "DEFAULT", "DELETE", "DESC", "DESCRIBE", "DISTINCT", "DIV", "DOUBLE", "DROP",
    //            "ELSE", "ELSEIF", "END", "ENGINE", "ESCAPE", "EXISTS", "EXIT", "EXPLAIN",
    //            "FALSE", "FLOAT", "FOR", "FOREIGN", "FROM", "FULLTEXT", "GROUP", "HAVING",
    //            "HIGH_PRIORITY", "HOUR", "IF", "IGNORE", "IN", "INDEX", "INNER", "INSERT",
    //            "INT", "INTEGER", "INTERVAL", "INTO", "IS", "JOIN", "KEY", "KEYS", "KILL",
    //            "LEFT", "LIKE", "LIMIT", "LOW_PRIORITY", "MATCH", "MEDIUMINT", "MOD", "MODIFY",
    //            "NOT", "NO_WRITE_TO_BINLOG", "NULL", "ON", "OPTIMIZE", "OR", "ORDER", "OUTER",
    //            "OVER", "PARTITION", "PRECISION", "PRIMARY", "PROCEDURE", "PURGE", "RANGE",
    //            "READ", "REFERENCES", "REGEXP", "RENAME", "REPLACE", "REQUIRE", "RESTRICT",
    //            "RETURN", "REVOKE", "RIGHT", "RLIKE", "SCHEMA", "SELECT", "SET", "SHOW",
    //            "SIGNAL", "SMALLINT", "SONAME", "SPATIAL", "SQL", "SQLEXCEPTION", "SQLSTATE",
    //            "SQLWARNING", "SQL_BIG_RESULT", "SQL_CALC_FOUND_ROWS", "SQL_SMALL_RESULT", "SSL",
    //            "STARTING", "STRAIGHT_JOIN", "TABLE", "TERMINATED", "THEN", "TIME", "TIMESTAMP",
    //            "TINYINT", "TO", "TRUNCATE", "TRUE", "UNION", "UNIQUE", "UNLOCK", "UPDATE",
    //            "USAGE", "USE", "USER", "USING", "VALUE", "VALUES", "VARBINARY", "VARCHAR",
    //            "VARCHARACTER", "VARYING", "VIEW", "WHEN", "WHERE", "WHILE", "WITH", "WRITE"
    //        };

    //        // 按表分组避免重复检查表名
    //        var tables = schema.GroupBy(s => s.TableName);

    //        foreach (var tableGroup in tables)
    //        {
    //            var tableName = tableGroup.Key;

    //            // 表名检查
    //            if (tableName != tableName.ToLower())
    //            {
    //                issues.Add((tableName, tableName, "表名应使用小写命名", ToSnakeCase(tableName)));
    //            }
    //            if (reservedWords.Contains(tableName.ToUpper()))
    //            {
    //                issues.Add((tableName, tableName, "表名使用了MySQL保留字", $"tbl_{tableName}"));
    //            }

    //            foreach (var obj in tableGroup)
    //            {
    //                var fullColName = $"{tableName}.{obj.ColumnName}";

    //                // ✅ 1. 转换期望的 snake_case 名称
    //                var expectedName = ToSnakeCase(obj.ColumnName);

    //                // ✅ 2. 如果当前名称 ≠ 期望名称，说明不符合规范
    //                if (obj.ColumnName != expectedName)
    //                {
    //                    issues.Add((fullColName, obj.ColumnName, "字段名应使用小写下划线命名（snake_case）", expectedName));
    //                }

    //                // ✅ 3. 保留字检查（可选）
    //                if (reservedWords.Contains(obj.ColumnName.ToUpper()))
    //                {
    //                    issues.Add((fullColName, obj.ColumnName, "字段名使用了MySQL保留字", $"col_{obj.ColumnName}"));
    //                }

    //                // ✅ 4. 主键检查
    //                if (obj.ColumnKey == "PRI" && obj.ColumnName != "id")
    //                {
    //                    issues.Add((fullColName, obj.ColumnName, "主键字段建议命名为 'id'", "id"));
    //                }

    //                // ✅ 5. 外键检查（仅对外键字段）
    //                if (obj.IsForeignKey && !obj.ColumnName.EndsWith("_id"))
    //                {
    //                    var suggested = expectedName.EndsWith("_id") ? expectedName : expectedName + "_id";
    //                    issues.Add((fullColName, obj.ColumnName, "外键字段应以 '_id' 结尾", suggested));
    //                }
    //            }
    //        }

    //        var result = new StringBuilder();

    //        // Markdown 标题
    //        result.AppendLine("# 📊 MySQL数据库命名规范检查报告");
    //        result.AppendLine();
    //        result.AppendLine($"📅 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    //        result.AppendLine();
    //        result.AppendLine("## 📌 检查摘要");
    //        result.AppendLine();
    //        result.AppendLine($"- **总表数**：{schema.Select(s => s.TableName).Distinct().Count()}");
    //        result.AppendLine($"- **总字段数**：{schema.Count}");
    //        result.AppendLine($"- **不合规项**：{issues.Count}");
    //        result.AppendLine($"- **合规率**：{(schema.Count > 0 ? (int)((double)(schema.Count - issues.Count) / schema.Count * 100) : 100)}%");
    //        result.AppendLine();

    //        if (issues.Count == 0)
    //        {
    //            result.AppendLine("✅ **所有命名均符合规范，恭喜！**");
    //        }
    //        else
    //        {
    //            result.AppendLine("## ❌ 不符合规范的命名");
    //            result.AppendLine();
    //            result.AppendLine("| 类型 | 对象 | 当前名称 | 问题 | 建议名称 |");
    //            result.AppendLine("|------|------|----------|------|----------|");

    //            foreach (var issue in issues)
    //            {
    //                var type = issue.Object.Contains(".") ? "字段" : "表名";
    //                result.AppendLine($"| {type} | `{issue.Object}` | `{issue.CurrentName}` | {issue.Problem} | `{issue.Suggestion}` |");
    //            }

    //            result.AppendLine();
    //            result.AppendLine("## 🔧 建议的 SQL 修复语句");
    //            result.AppendLine("```sql");
    //            foreach (var issue in issues)
    //            {
    //                if (issue.Object.Contains("."))
    //                {
    //                    var parts = issue.Object.Split('.');
    //                    result.AppendLine($"ALTER TABLE `{parts[0]}` CHANGE COLUMN `{parts[1]}` `{issue.Suggestion}` {GetColumnDataType(schema, parts[0], parts[1])};");
    //                }
    //                else
    //                {
    //                    result.AppendLine($"ALTER TABLE `{issue.CurrentName}` RENAME TO `{issue.Suggestion}`;");
    //                }
    //            }
    //            result.AppendLine("```");
    //        }

    //        return result.ToString();
    //    }

    //    // 获取字段的数据类型
    //    private string GetColumnDataType(List<DbObject> schema, string tableName, string columnName)
    //    {
    //        var column = schema.FirstOrDefault(c => c.TableName == tableName && c.ColumnName == columnName);
    //        if (column != null)
    //        {
    //            // 简化的数据类型返回，实际应用中可能需要更复杂的处理
    //            return column.DataType;
    //        }
    //        return "VARCHAR(255)";
    //    }

    //    /// <summary>
    //    /// 将 PascalCase/camelCase 转为 snake_case
    //    /// </summary>
    //    public string ToSnakeCase(string input)
    //    {
    //        if (string.IsNullOrEmpty(input))
    //            return input;

    //        // 在小写/数字后接大写时插入下划线：user(ID) → user_(ID)
    //        var result = Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2");

    //        // 在大写后接大写+小写时插入下划线：(HTML)Parser → (HTML)_Parser
    //        result = Regex.Replace(result, @"([A-Z])([A-Z][a-z])", "$1_$2");

    //        // 全部转为小写
    //        return result.ToLower();
    //    }
    //}

    //// 数据模型
    //public class DbObject
    //{
    //    public string TableName { get; set; }
    //    public string ColumnName { get; set; }

    //    public string ColumnType { get; set; } // 存储完整类型如 varchar(16), decimal(10,2)
    //    public string DataType { get; set; }
    //    public bool IsNullable { get; set; }
    //    public string ColumnKey { get; set; }
    //    public string ColumnDefault { get; set; }
    //    public string Extra { get; set; }
    //    public string ConstraintType { get; set; }
    //    public bool IsForeignKey { get; set; }   // 新增：显式标记外键
    //}
}

