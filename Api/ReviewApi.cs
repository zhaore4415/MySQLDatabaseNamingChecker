using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DBCheckAI
{
    /// <summary>
    /// /api/review - AI 代码/SQL 审查接口
    /// </summary>
    public static class ReviewApi
    {
        /// <summary>
        /// 注册 /api/review 接口
        /// </summary>
        public static void Map(WebApplication app)
        {
            // POST /api/review - 代码/SQL 审查
            app.MapPost("/api/review", async (
                ReviewRequest request,
                IAIService aiService,
                IOptions<AIConfig> configOptions,
                HttpContext httpContext) =>
            {
                // 1. 鉴权
                var config = configOptions.Value;
                var authHeader = httpContext.Request.Headers.Authorization.ToString();
                if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
                    authHeader.Substring(7) != config.ApiToken)
                {
                    return Results.Json(new ReviewResponse
                    {
                        Error = "未授权：请提供有效的 Bearer Token"
                    }, statusCode: 401);
                }

                // 2. 参数校验
                if (request.Files == null || request.Files.Count == 0)
                {
                    return Results.Json(new ReviewResponse
                    {
                        Error = "请求中未包含文件"
                    }, statusCode: 400);
                }

                try
                {
                    // 3. 构建 Prompt
                    string prompt;
                    if (request.Type == "sql")
                    {
                        // 判断 SQL 是 DML 还是 DDL
                        var sqlContent = string.Join("\n", request.Files.Select(f => f.Content));
                        if (IsDmlSql(sqlContent))
                        {
                            prompt = ReviewPromptBuilder.BuildSqlQueryReviewPrompt(request.Files);
                        }
                        else
                        {
                            prompt = ReviewPromptBuilder.BuildSqlReviewPrompt(request.Files);
                        }
                    }
                    else
                    {
                        prompt = ReviewPromptBuilder.BuildCodeReviewPrompt(request.Files);
                    }

                    // 4. 调用 AI
                    var aiResponse = await aiService.GetResponseAsync(prompt);

                    // 5. 解析 AI 返回的 JSON
                    var result = ParseAIResponse(aiResponse, request.Files);

                    return Results.Json(result);
                }
                catch (Exception ex)
                {
                    return Results.Json(new ReviewResponse
                    {
                        Error = $"检查过程中出现错误: {ex.Message}"
                    }, statusCode: 500);
                }
            });
        }

        /// <summary>
        /// 从 diff 内容中提取新增的行（以 + 开头），判断是否包含 DML
        /// </summary>
        private static bool IsDmlSql(string diffContent)
        {
            // diff 格式中，+ 开头的行是新增行（跳过 +++ 文件头标记）
            var addedLines = diffContent.Split('\n')
                .Where(l => l.StartsWith('+') && !l.StartsWith("+++"))
                .Select(l => l.TrimStart('+').Trim());

            var dmlKeywords = new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "MERGE" };
            return addedLines.Any(line => dmlKeywords.Any(k => line.ToUpperInvariant().Contains(k)));
        }

        /// <summary>
        /// 解析 AI 返回的 JSON，生成可读的 Markdown 报告
        /// </summary>
        private static ReviewResponse ParseAIResponse(string aiResponse, List<ReviewFile> files)
        {
            // 尝试提取 JSON 块（AI 可能用 ```json 包裹）
            var jsonMatch = Regex.Match(aiResponse, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
            var jsonText = jsonMatch.Success ? jsonMatch.Groups[1].Value : aiResponse;

            // 清理可能的前缀后缀
            jsonText = jsonText.Trim();
            var startIdx = jsonText.IndexOf('{');
            var endIdx = jsonText.LastIndexOf('}');
            if (startIdx >= 0 && endIdx > startIdx)
            {
                jsonText = jsonText.Substring(startIdx, endIdx - startIdx + 1);
            }

            try
            {
                var aiResult = JsonSerializer.Deserialize<AIReviewResult>(jsonText);
                if (aiResult != null)
                {
                    var issues = (aiResult.Issues ?? new List<AIReviewIssue>()).Select(i => new ReviewIssue
                    {
                        File = i.File ?? files.FirstOrDefault()?.Path ?? "",
                        Line = i.Line,
                        Severity = i.Severity ?? "warning",
                        Category = i.Category ?? "",
                        Message = i.Message ?? "",
                        Suggestion = i.Suggestion ?? ""
                    }).ToList();

                    // 生成可读的 Markdown 报告，而非原始 JSON
                    var md = new StringBuilder();
                    md.AppendLine("## AI 审查报告");
                    md.AppendLine();
                    md.AppendLine($"**评分**: {aiResult.Score}/100");
                    md.AppendLine($"**问题数**: {issues.Count}");
                    md.AppendLine();
                    md.AppendLine("---");
                    md.AppendLine();

                    if (issues.Count > 0)
                    {
                        md.AppendLine("### 发现的问题");
                        md.AppendLine();
                        for (int i = 0; i < issues.Count; i++)
                        {
                            var issue = issues[i];
                            var severityIcon = issue.Severity == "critical" ? "🔴" :
                                               issue.Severity == "warning" ? "⚠️" : "ℹ️";
                            md.AppendLine($"{i + 1}. {severityIcon} **[{issue.Severity}]** {issue.Category}");
                            md.AppendLine($"   - **位置**: {issue.File}{(issue.Line.HasValue ? $" 第 {issue.Line} 行" : "")}");
                            md.AppendLine($"   - **问题**: {issue.Message}");
                            if (!string.IsNullOrEmpty(issue.Suggestion))
                            {
                                md.AppendLine($"   - **建议**: {issue.Suggestion}");
                            }
                            md.AppendLine();
                        }
                    }
                    else
                    {
                        md.AppendLine("✅ **未发现明显问题**");
                        md.AppendLine();
                    }

                    md.AppendLine("---");
                    md.AppendLine();
                    md.AppendLine($"*审查时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");

                    return new ReviewResponse
                    {
                        Score = aiResult.Score,
                        TotalIssues = issues.Count,
                        Issues = issues,
                        ReportMarkdown = md.ToString()
                    };
                }
            }
            catch (JsonException)
            {
                // JSON 解析失败，兜底处理
            }

            // 兜底：返回原始文本
            return new ReviewResponse
            {
                Score = null,
                TotalIssues = 0,
                Issues = new List<ReviewIssue>(),
                ReportMarkdown = $"## AI 审查报告（原始输出）\n\n{aiResponse}",
                Error = "AI 返回格式不符合预期 JSON 格式，已返回原始文本"
            };
        }
    }
}
