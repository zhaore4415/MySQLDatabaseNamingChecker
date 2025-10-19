using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace MySQLDatabaseNamingChecker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🔍 MySQL数据库命名规范检查工具");
            Console.WriteLine("================================");

            // 1. 配置MySQL数据库连接（请修改为你的连接字符串）
            string connectionString = "Server=localhost;Database=cssao_new;Uid=root;Pwd=cssao888;";
            //Server=localhost;Database=cssao_new;Uid=root;Pwd=cssao888;
            // 2. 定义命名规范
            string namingRules = @"
## MySQL数据库命名规范（示例）

✅ 表名规范：
- 使用复数名词，如 users, orders
- 使用小写 + 下划线：user_profile, order_item
- 不允许使用大写字母或驼峰
- 长度不超过64字符

✅ 字段名规范：
- 使用小驼峰命名法（lowerCamelCase）：userId, createTime, totalAmount
- 主键必须叫 id
- 外键必须以 _id 结尾，如 user_id, order_id
- 创建时间字段叫 create_time，更新时间叫 update_time
- 布尔字段用 is_xxx，如 is_active, is_deleted

❌ 禁止：
- 使用拼音（如 yonghu）
- 使用MySQL保留字（如 order, user, group 等）
- 使用空格或特殊字符
- 使用MySQL不支持的字符

✅ 索引命名规范：
- 主键：PRIMARY KEY (id)
- 普通索引：idx_字段名，如 idx_user_id
- 唯一索引：uk_字段名，如 uk_email
- 复合索引：idx_字段1_字段2，如 idx_user_id_status
";

            try
            {
                // 3. 提取MySQL数据库表结构
                Console.WriteLine("📊 正在提取MySQL数据库表结构...");
                var schema = await GetMySQLDatabaseSchemaAsync(connectionString);

                if (schema.Count == 0)
                {
                    Console.WriteLine("❌ 未找到任何表结构，请检查数据库连接！");
                    return;
                }

                Console.WriteLine($"✅ 成功提取 {schema.Count} 个表字段");

                // 4. 生成检查报告
                Console.WriteLine("\n🔍 正在检查命名规范...");
                var report = await CheckNamingWithAIAsync(schema, namingRules);

                // 5. 输出报告
                Console.WriteLine("\n📋 检查结果：");
                Console.WriteLine("=================");
                Console.WriteLine(report);

                // 6. 保存报告到文件
                SaveReportToFile(report);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 检查过程中出现错误：{ex.Message}");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        // 提取MySQL数据库表和字段信息
        public static async Task<List<DbObject>> GetMySQLDatabaseSchemaAsync(string connectionString)
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
                            Extra = reader["EXTRA"]?.ToString()
                        });
                    }
                }
            }

            return result;
        }

        // 调用通义千问自动检查命名规范
        public static async Task<string> CheckNamingWithAIAsync(List<DbObject> schema, string namingRules)
        {
            // 构造输入给 AI 的提示词
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

            sb.AppendLine();
            sb.AppendLine("## 要求");
            sb.AppendLine("1. 列出所有不符合规范的表名和字段名");
            sb.AppendLine("2. 说明违反了哪条规则");
            sb.AppendLine("3. 建议修正后的名称");
            sb.AppendLine("4. 格式：| 对象 | 当前名称 | 问题 | 建议名称 |");
            sb.AppendLine("5. 最后给出总体评分（如 85/100）");
            sb.AppendLine("6. 如果问题严重，建议生成 ALTER TABLE 语句");

            var prompt = sb.ToString();

            // 模拟调用AI（在实际项目中替换为真实的API调用）
            // 在实际项目中，这里应该调用通义千问API
            return SimulateAIResponse(schema, namingRules);
        }

        // 模拟AI响应（在实际项目中替换为真实的API调用）
        // 模拟AI响应（输出为 Markdown 格式）
        public static string SimulateAIResponse(List<DbObject> schema, string rules)
        {
            var issues = new List<(string Object, string CurrentName, string Problem, string Suggestion)>();

            // PostgreSQL保留字列表（部分）
            var reservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ALL", "ANALYSE", "ANALYZE", "AND", "ANY", "ARRAY", "AS", "ASC",
        "ASYMMETRIC", "BOTH", "CASE", "CAST", "CHECK", "COLLATE", "COLUMN",
        "CONCURRENTLY", "CONSTRAINT", "CREATE", "CROSS", "CURRENT_CATALOG",
        "CURRENT_DATE", "CURRENT_ROLE", "CURRENT_SCHEMA", "CURRENT_TIME",
        "CURRENT_TIMESTAMP", "CURRENT_USER", "DEFAULT", "DEFERRABLE",
        "DESC", "DISTINCT", "DO", "ELSE", "END", "EXCEPT", "FALSE", "FETCH",
        "FOR", "FOREIGN", "FROM", "GRANT", "GROUP", "HAVING", "ILIKE",
        "IN", "INITIALLY", "INTERSECT", "INTO", "IS", "ISNULL", "JOIN",
        "LATERAL", "LEADING", "LEFT", "LIKE", "LIMIT", "LOCALTIME",
        "LOCALTIMESTAMP", "NATURAL", "NOT", "NOTNULL", "NULL", "OFFSET",
        "ON", "ONLY", "OR", "ORDER", "OUTER", "OVERLAPS", "PLACING",
        "PRIMARY", "REFERENCES", "RETURNING", "RIGHT", "SELECT", "SESSION_USER",
        "SIMILAR", "SOME", "SYMMETRIC", "TABLE", "THEN", "TO", "TRAILING",
        "TRUE", "UNION", "UNIQUE", "USER", "USING", "VARIADIC", "VERBOSE",
        "WHEN", "WHERE", "WINDOW", "WITH"
    };

            foreach (var obj in schema)
            {
                // 检查表名：应为小写下划线
                if (obj.TableName != obj.TableName.ToLower() || !obj.TableName.Contains("_"))
                {
                    var suggested = ToSnakeCase(obj.TableName);
                    issues.Add((obj.TableName, obj.TableName, "表名应使用小写+下划线命名法", suggested));
                }

                // 检查表名是否为保留字
                if (reservedWords.Contains(obj.TableName))
                {
                    issues.Add((obj.TableName, obj.TableName, "表名使用了PostgreSQL保留字", $"tbl_{obj.TableName}"));
                }

                // 检查字段名：应为小写下划线
                if (obj.ColumnName != obj.ColumnName.ToLower() || obj.ColumnName.Contains("_") == false)
                {
                    var suggested = ToSnakeCase(obj.ColumnName);
                    issues.Add(($"{obj.TableName}.{obj.ColumnName}", obj.ColumnName, "字段名应使用小写+下划线命名法", suggested));
                }

                // 检查字段名是否为保留字
                if (reservedWords.Contains(obj.ColumnName))
                {
                    issues.Add(($"{obj.TableName}.{obj.ColumnName}", obj.ColumnName, "字段名使用了PostgreSQL保留字", $"col_{obj.ColumnName}"));
                }

                // 检查外键命名规范
                if (obj.ConstraintType == "FOREIGN KEY" && !obj.ColumnName.EndsWith("_id"))
                {
                    var suggested = ToSnakeCase(obj.ColumnName) + "_id";
                    issues.Add(($"{obj.TableName}.{obj.ColumnName}", obj.ColumnName, "外键字段应以 _id 结尾", suggested));
                }
            }

            var result = new StringBuilder();

            // Markdown 标题
            result.AppendLine("# 📊 PostgreSQL数据库命名规范检查报告");
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
                        result.AppendLine($"-- 修复字段: ALTER TABLE \"{parts[0]}\" RENAME COLUMN \"{parts[1]}\" TO \"{issue.Suggestion}\";");
                    }
                    else
                    {
                        result.AppendLine($"-- 修复表名: ALTER TABLE \"{issue.CurrentName}\" RENAME TO \"{issue.Suggestion}\";");
                    }
                }
                result.AppendLine("```");
            }

            return result.ToString();
        }

        /// <summary>
        /// 更智能地将 PascalCase/camelCase 转为 snake_case
        /// 支持处理缩写词，如 HTTPCode -> http_code
        /// </summary>
        public static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // 在小写字母后接大写字母的地方插入下划线（如: UserN -> User_N）
            // 在大写字母后接小写字母的地方插入下划线（如: HTTPResponse -> HTTP_Response）
            var result = Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2");
            // 全部转为小写
            return result.ToLower();
        }
        // 将驼峰命名转换为下划线命名
        //private static string ConvertToSnakeCase(string camelCase)
        //{
        //    var result = new StringBuilder();
        //    for (int i = 0; i < camelCase.Length; i++)
        //    {
        //        if (char.IsUpper(camelCase[i]) && i > 0)
        //        {
        //            result.Append("_");
        //        }
        //        result.Append(char.ToLower(camelCase[i]));
        //    }
        //    return result.ToString();
        //}

        // 保存报告到文件
        public static void SaveReportToFile(string report)
        {
            string fileName = $"DatabaseNamingReport_{DateTime.Now:yyyyMMdd_HHmmss}.md";
            System.IO.File.WriteAllText(fileName, report);
            Console.WriteLine($"📄 报告已保存到：{fileName}");
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

        public string ConstraintType { get; set; } // 👈 必须包含这个属性！
    }
}

