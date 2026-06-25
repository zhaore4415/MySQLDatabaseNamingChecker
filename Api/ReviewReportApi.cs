using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using DBCheckAI.Models;
using DBCheckAI.Services;

namespace DBCheckAI
{
    /// <summary>
    /// /api/review/report - AI 审查结果上报与查询接口
    /// </summary>
    public static class ReviewReportApi
    {
        /// <summary>
        /// 注册 /api/review/report 和查询接口
        /// </summary>
        public static void Map(WebApplication app)
        {
            // POST /api/review/report - 上报审查结果（数据持久化）
            app.MapPost("/api/review/report", async (
                ReviewReportRequest request,
                IReviewReportService reportService,
                IOptions<AIConfig> configOptions,
                HttpContext httpContext) =>
            {
                // 鉴权
                var config = configOptions.Value;
                var authHeader = httpContext.Request.Headers.Authorization.ToString();
                if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
                    authHeader.Substring(7) != config.ApiToken)
                {
                    return Results.Json(new { Error = "未授权：请提供有效的 Bearer Token" }, statusCode: 401);
                }

                // 参数校验
                if (string.IsNullOrEmpty(request.Repo) || request.PrNumber <= 0)
                {
                    return Results.Json(new { Error = "缺少必要参数：repo 和 pr_number" }, statusCode: 400);
                }

                try
                {
                    var id = await reportService.ReportAsync(request);
                    return Results.Json(new { Success = true, Id = id });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { Error = $"上报失败: {ex.Message}" }, statusCode: 500);
                }
            });

            // GET /api/review/history - 查询审查历史
            app.MapGet("/api/review/history", async (
                IReviewReportService reportService,
                string? repo,
                DateTime? startDate,
                DateTime? endDate,
                int page = 1,
                int pageSize = 20) =>
            {
                var data = await reportService.GetHistoryAsync(repo, startDate, endDate, page, pageSize);
                return Results.Json(data);
            });

            // GET /api/review/ranking/repo - 仓库排名
            app.MapGet("/api/review/ranking/repo", async (
                IReviewReportService reportService,
                DateTime? startDate,
                int top = 10) =>
            {
                var data = await reportService.GetRepoRankingAsync(startDate, top);
                return Results.Json(data);
            });

            // GET /api/review/ranking/user - 个人排名
            app.MapGet("/api/review/ranking/user", async (
                IReviewReportService reportService,
                string? repo,
                DateTime? startDate,
                int top = 10) =>
            {
                var data = await reportService.GetUserRankingAsync(repo, startDate, top);
                return Results.Json(data);
            });

            // GET /api/review/trend/{repo} - 仓库趋势
            app.MapGet("/api/review/trend/{repo}", async (
                string repo,
                DateTime startDate,
                DateTime endDate,
                IReviewReportService reportService) =>
            {
                var data = await reportService.GetRepoTrendAsync(repo, startDate, endDate);
                return Results.Json(data);
            });
        }
    }
}
