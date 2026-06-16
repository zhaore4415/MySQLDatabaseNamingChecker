using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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
            // 读取配置
            builder.Services.Configure<AIConfig>(builder.Configuration.GetSection("AIConfig"));
            
            // 注册HTTP客户端
            builder.Services.AddHttpClient<IAIService, AIService>();
            
            // 注册数据库服务
            builder.Services.AddSingleton<DatabaseService>();
            
            //// 配置应用程序监听的端口，避免端口冲突
            //builder.WebHost.ConfigureKestrel(options =>
            //{
            //    options.ListenLocalhost(5001); // 使用5001端口
            //});

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

            // ========== 新增：API 审查接口 ==========
            MapReviewApi(app);

            app.Run();
        }

        /// <summary>
        /// 注册 /api/review 接口
        /// </summary>
        private static void MapReviewApi(WebApplication app)
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
                        prompt = ReviewPromptBuilder.BuildSqlReviewPrompt(request.Files);
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
        /// 解析 AI 返回的 JSON，失败时返回原始文本作为 Markdown 报告
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
                    return new ReviewResponse
                    {
                        Score = aiResult.Score,
                        TotalIssues = aiResult.Issues?.Count ?? 0,
                        Issues = (aiResult.Issues ?? new List<AIReviewIssue>()).Select(i => new ReviewIssue
                        {
                            File = i.File ?? files.FirstOrDefault()?.Path ?? "",
                            Line = i.Line,
                            Severity = i.Severity ?? "warning",
                            Category = i.Category ?? "",
                            Message = i.Message ?? "",
                            Suggestion = i.Suggestion ?? ""
                        }).ToList(),
                        ReportMarkdown = $"## AI 审查报告\n\n**评分**: {aiResult.Score}/100\n\n{aiResponse}"
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


