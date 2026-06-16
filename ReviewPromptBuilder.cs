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

            sb.AppendLine("你是一名资深 C# 架构师。请对以下代码变更进行审查。");
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
            sb.AppendLine("待审查代码：");
            sb.AppendLine();

            AppendFiles(sb, files);

            return sb.ToString();
        }

        /// <summary>
        /// 构建 SQL 审查 Prompt
        /// </summary>
        public static string BuildSqlReviewPrompt(List<ReviewFile> files)
        {
            var sb = new StringBuilder();

            sb.AppendLine("你是一名数据库专家。请审查以下 SQL 脚本。");
            sb.AppendLine();
            sb.AppendLine("检查维度：");
            sb.AppendLine("1. 命名规范（snake_case、保留字、审计字段完整性）");
            sb.AppendLine("2. 脚本优化（慢 SQL 风险、N+1 查询、大事务）");
            sb.AppendLine("3. 静态分析（缺索引、全表扫描、批量操作在循环内）");
            sb.AppendLine();
            sb.AppendLine("请按以下 JSON 格式输出，不要输出任何其他解释：");
            sb.AppendLine("{");
            sb.AppendLine("  \"score\": 85,");
            sb.AppendLine("  \"issues\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"file\": \"文件路径\",");
            sb.AppendLine("      \"line\": 45,");
            sb.AppendLine("      \"severity\": \"warning\",");
            sb.AppendLine("      \"category\": \"命名规范|脚本优化|静态分析\",");
            sb.AppendLine("      \"message\": \"问题描述\",");
            sb.AppendLine("      \"suggestion\": \"改进建议\"");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("注意：");
            sb.AppendLine("- 如果 SQL 没有明显问题，score 可以打 90-100，issues 数组可为空");
            sb.AppendLine("- severity 只能是 warning 或 info");
            sb.AppendLine("- line 如果无法确定，可填 null");
            sb.AppendLine("- 不要返回 Markdown 格式，只返回纯 JSON");
            sb.AppendLine();
            sb.AppendLine("待审查 SQL：");
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
                if (bytes > MaxFileSize)
                {
                    sb.AppendLine($"--- 文件: {file.Path} (内容超过 50KB，已跳过) ---");
                    sb.AppendLine();
                    continue;
                }

                sb.AppendLine($"--- 文件: {file.Path} ---");
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
