using System.Text;

namespace DBCheckAI
{
    /// <summary>
    /// 审查 Prompt 构建器
    /// </summary>
    public class ReviewPromptBuilder
    {
        private const int MaxFileSize = 50 * 1024; // 50KB

        /// <summary>
        /// 构建代码审查 Prompt
        /// </summary>
        public static string BuildCodeReviewPrompt(List<ReviewFile> files)
        {
            var sb = new StringBuilder();

            sb.AppendLine("你是一名资深 C# 架构师。以下内容为本次 Git 变更的差异（diff 格式）。");
            sb.AppendLine("- '+' 开头的行表示本次新增的代码");
            sb.AppendLine("- '-' 开头的行表示本次删除的代码");
            sb.AppendLine("- 请重点关注 '+' 新增行的代码质量");
            sb.AppendLine();
            sb.AppendLine("评估维度（每项 0-20 分，总分 100）：");
            sb.AppendLine("1. 设计模式符合度（SOLID 原则、常用设计模式应用）");
            sb.AppendLine("2. 代码复用性（是否有重复代码、DRY 原则遵循）");
            sb.AppendLine("3. 架构合理性（分层清晰、依赖关系合理、耦合度）");
            sb.AppendLine("4. 可维护性（命名规范、可读性、注释完整性）");
            sb.AppendLine("5. 最佳实践遵循度（C# idiomatic 写法、.NET 框架最佳实践）");
            sb.AppendLine();
            sb.AppendLine("请按以下 JSON 格式输出，不要输出任何其他解释：");
            sb.AppendLine("{");
            sb.AppendLine("  \"score\": 85,");
            sb.AppendLine("  \"issues\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"file\": \"文件路径\",");
            sb.AppendLine("      \"line\": 45,");
            sb.AppendLine("      \"severity\": \"warning\",");
            sb.AppendLine("      \"category\": \"设计模式|代码复用|架构|可维护性|最佳实践\",");
            sb.AppendLine("      \"message\": \"问题描述\",");
            sb.AppendLine("      \"suggestion\": \"改进建议\"");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("注意：");
            sb.AppendLine("- 如果代码没有明显问题，score 可以打 90-100，issues 数组可为空");
            sb.AppendLine("- severity 只能是 warning 或 info");
            sb.AppendLine("- line 如果无法确定，可填 null");
            sb.AppendLine("- 不要返回 Markdown 格式，只返回纯 JSON");
            sb.AppendLine();
            sb.AppendLine("本次变更差异（diff）：");
            sb.AppendLine();

            AppendFiles(sb, files);

            return sb.ToString();
        }

        /// <summary>
        /// 构建 SQL 审查 Prompt（DDL 结构变更脚本）
        /// 不检查命名规范，只关注性能和安全风险
        /// </summary>
        public static string BuildSqlReviewPrompt(List<ReviewFile> files)
        {
            var sb = new StringBuilder();

            sb.AppendLine("你是一名数据库专家。以下内容为本次 Git 变更的 SQL 差异（diff 格式）。");
            sb.AppendLine("- '+' 开头的行表示本次新增的 SQL");
            sb.AppendLine("- '-' 开头的行表示本次删除的 SQL");
            sb.AppendLine("- 请重点关注 '+' 新增行的 SQL 质量");
            sb.AppendLine();
            sb.AppendLine("重要：不检查字段命名规范，不检查审计字段。只关注以下性能和安全风险。");
            sb.AppendLine();
            sb.AppendLine("检查维度：");
            sb.AppendLine("1. ALTER TABLE 大表锁表风险（如果数据量很大，建议使用 Online DDL 或 pt-online-schema-change）");
            sb.AppendLine("2. 字段类型/长度合理性（如 VARCHAR(1000) 过长、标志位未 NOT NULL DEFAULT 0）");
            sb.AppendLine("3. 新增字段是否缺少索引（如 CustomerId、WorkOrderCode 等常用查询字段）");
            sb.AppendLine("4. 标志位字段（如 IsPickupGoods、IsCP）是否 NOT NULL DEFAULT 0（NULL 会影响索引效率）");
            sb.AppendLine("5. 排序规则兼容性（如 utf8mb4_0900_ai_ci 在 MySQL 5.7 不兼容）");
            sb.AppendLine("6. 多表重复添加相同字段（如 6 个表都加 CustomerId、CustomerName）是否建议抽取公共表");
            sb.AppendLine("7. 重命名字段是否用 CHANGE（MySQL 5.7 会重建表；MySQL 8.0 应优先用 RENAME COLUMN）");
            sb.AppendLine();
            sb.AppendLine("请按以下 JSON 格式输出，不要输出任何其他解释：");
            sb.AppendLine("{");
            sb.AppendLine("  \"score\": 85,");
            sb.AppendLine("  \"issues\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"file\": \"文件路径\",");
            sb.AppendLine("      \"line\": 45,");
            sb.AppendLine("      \"severity\": \"warning\",");
            sb.AppendLine("      \"category\": \"锁表风险|字段类型|缺索引|标志位|兼容性|重复字段|重命名\",");
            sb.AppendLine("      \"message\": \"问题描述\",");
            sb.AppendLine("      \"suggestion\": \"改进建议\"");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("注意：");
            sb.AppendLine("- 如果 SQL 没有明显风险，score 可以打 90-100，issues 数组可为空");
            sb.AppendLine("- severity 只能是 warning 或 info");
            sb.AppendLine("- line 如果无法确定，可填 null");
            sb.AppendLine("- 不要返回 Markdown 格式，只返回纯 JSON");
            sb.AppendLine();
            sb.AppendLine("本次变更差异（diff）：");
            sb.AppendLine();

            AppendFiles(sb, files);

            return sb.ToString();
        }

        /// <summary>
        /// 构建 SQL 查询审查 Prompt（DML 查询/操作脚本）
        /// 不检查命名规范，只关注性能风险
        /// </summary>
        public static string BuildSqlQueryReviewPrompt(List<ReviewFile> files)
        {
            var sb = new StringBuilder();

            sb.AppendLine("你是一名数据库专家。以下内容为本次 Git 变更的 SQL 差异（diff 格式）。");
            sb.AppendLine("- '+' 开头的行表示本次新增或修改的 SQL");
            sb.AppendLine("- '-' 开头的行表示本次删除的 SQL");
            sb.AppendLine("- 请重点关注 '+' 新增行的性能和安全问题");
            sb.AppendLine();
            sb.AppendLine("重要：不检查字段命名规范。只关注以下性能风险。");
            sb.AppendLine();
            sb.AppendLine("检查维度：");
            sb.AppendLine("1. 慢 SQL 风险（全表扫描、无索引 WHERE、大量 JOIN、深分页 LIMIT 1000000,10）");
            sb.AppendLine("2. N+1 查询（循环中重复查询数据库）");
            sb.AppendLine("3. 大事务（事务包含过多操作或长时间不提交）");
            sb.AppendLine("4. 批量操作在循环内（逐条 INSERT/UPDATE 应改为批量）");
            sb.AppendLine("5. 缺少索引（WHERE、JOIN、ORDER BY 字段未建索引）");
            sb.AppendLine("6. SELECT * 浪费（只查询需要的字段）");
            sb.AppendLine();
            sb.AppendLine("请按以下 JSON 格式输出，不要输出任何其他解释：");
            sb.AppendLine("{");
            sb.AppendLine("  \"score\": 85,");
            sb.AppendLine("  \"issues\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"file\": \"文件路径\",");
            sb.AppendLine("      \"line\": 45,");
            sb.AppendLine("      \"severity\": \"warning\",");
            sb.AppendLine("      \"category\": \"慢SQL|N+1|大事务|批量操作|缺索引|SELECT*\",");
            sb.AppendLine("      \"message\": \"问题描述\",");
            sb.AppendLine("      \"suggestion\": \"改进建议\"");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("注意：");
            sb.AppendLine("- 如果 SQL 没有明显风险，score 可以打 90-100，issues 数组可为空");
            sb.AppendLine("- severity 只能是 warning 或 info");
            sb.AppendLine("- line 如果无法确定，可填 null");
            sb.AppendLine("- 不要返回 Markdown 格式，只返回纯 JSON");
            sb.AppendLine();
            sb.AppendLine("本次变更差异（diff）：");
            sb.AppendLine();

            AppendFiles(sb, files);

            return sb.ToString();
        }

        /// <summary>
        /// 过滤超大文件并拼接内容
        /// </summary>
        private static void AppendFiles(StringBuilder sb, List<ReviewFile> files)
        {
            int included = 0;
            foreach (var file in files)
            {
                var bytes = Encoding.UTF8.GetByteCount(file.Content);

                sb.AppendLine($"--- 文件: {file.Path} ---");

                if (bytes > MaxFileSize)
                {
                    sb.AppendLine("（差异内容超过 50KB，已跳过）");
                    sb.AppendLine();
                    continue;
                }

                sb.AppendLine(file.Content);
                sb.AppendLine();
                included++;
            }

            if (included == 0)
            {
                sb.AppendLine("（无有效文件内容可审查）");
            }
        }
    }
}