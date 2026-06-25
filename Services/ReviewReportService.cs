using Microsoft.EntityFrameworkCore;
using DBCheckAI.Data;
using DBCheckAI.Models;

namespace DBCheckAI.Services
{
    public interface IReviewReportService
    {
        Task<long> ReportAsync(ReviewReportRequest request);
        Task<List<ReviewHistoryResponse>> GetHistoryAsync(string? repo, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 20);
        Task<List<RankingItem>> GetRepoRankingAsync(DateTime? startDate, int top = 10);
        Task<List<RankingItem>> GetUserRankingAsync(string? repo, DateTime? startDate, int top = 10);
        Task<List<RepoTrendData>> GetRepoTrendAsync(string repo, DateTime startDate, DateTime endDate);
    }

    public class ReviewReportService : IReviewReportService
    {
        private readonly ReviewDbContext _db;

        public ReviewReportService(ReviewDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 上报审查结果，实时计算健壮性分并更新汇总表
        /// </summary>
        public async Task<long> ReportAsync(ReviewReportRequest request)
        {
            // 1. 保存审查记录
            var record = new AiReviewRecord
            {
                Repo = request.Repo,
                PrNumber = request.PrNumber,
                SourceBranch = request.SourceBranch,
                TargetBranch = request.TargetBranch,
                CommitSha = request.CommitSha,
                ReviewType = request.ReviewType,
                Score = request.Score,
                TotalIssues = request.TotalIssues,
                ReportMarkdown = request.ReportMarkdown,
                AiModel = request.AiModel,
                TokenUsed = request.TokenUsed,
                DurationMs = request.DurationMs,
                Reviewer = request.Reviewer,
                CreatedAt = DateTime.Now
            };

            _db.AiReviewRecords.Add(record);
            await _db.SaveChangesAsync();

            // 2. 保存问题明细
            if (request.Issues?.Any() == true)
            {
                foreach (var issue in request.Issues)
                {
                    _db.AiReviewIssues.Add(new AiReviewIssue
                    {
                        ReviewId = record.Id,
                        FilePath = issue.FilePath,
                        LineNumber = issue.LineNumber,
                        Severity = issue.Severity,
                        Category = issue.Category,
                        Message = issue.Message,
                        Suggestion = issue.Suggestion
                    });
                }
                await _db.SaveChangesAsync();
            }

            // 3. 实时更新汇总统计（仓库 + 个人）
            var statDate = DateTime.Now.Date;
            await UpdateRepoStatsAsync(request.Repo, statDate);
            if (!string.IsNullOrEmpty(request.Reviewer))
            {
                await UpdateUserStatsAsync(request.Reviewer, request.Repo, statDate);
            }

            return record.Id;
        }

        /// <summary>
        /// 计算健壮性分权重
        /// </summary>
        private static decimal GetRobustWeight(int? score)
        {
            if (!score.HasValue) return 1m;
            return score.Value switch
            {
                >= 90 => 1.0m,
                >= 80 => 0.9m,
                >= 60 => 0.8m,
                _ => 0.5m
            };
        }

        /// <summary>
        /// 更新仓库日统计
        /// </summary>
        private async Task UpdateRepoStatsAsync(string repo, DateTime statDate)
        {
            // 查询该仓库当日的所有记录
            var records = await _db.AiReviewRecords
                .Where(r => r.Repo == repo && r.CreatedAt.Date == statDate && r.Score.HasValue)
                .ToListAsync();

            if (!records.Any()) return;

            var count = records.Count;
            var avgScore = records.Average(r => (decimal)r.Score!.Value);
            var robustScore = records.Sum(r => r.Score!.Value * GetRobustWeight(r.Score)) / count;
            var totalIssues = records.Sum(r => r.TotalIssues);

            var stat = await _db.AiReviewRepoStats
                .FirstOrDefaultAsync(s => s.Repo == repo && s.StatDate == statDate);

            if (stat == null)
            {
                _db.AiReviewRepoStats.Add(new AiReviewRepoStat
                {
                    Repo = repo,
                    StatDate = statDate,
                    ReviewCount = count,
                    AvgScore = avgScore,
                    RobustScore = robustScore,
                    TotalIssues = totalIssues
                });
            }
            else
            {
                stat.ReviewCount = count;
                stat.AvgScore = avgScore;
                stat.RobustScore = robustScore;
                stat.TotalIssues = totalIssues;
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// 更新个人日统计
        /// </summary>
        private async Task UpdateUserStatsAsync(string username, string repo, DateTime statDate)
        {
            var records = await _db.AiReviewRecords
                .Where(r => r.Reviewer == username && r.Repo == repo
                            && r.CreatedAt.Date == statDate && r.Score.HasValue)
                .ToListAsync();

            if (!records.Any()) return;

            var count = records.Count;
            var avgScore = records.Average(r => (decimal)r.Score!.Value);
            var robustScore = records.Sum(r => r.Score!.Value * GetRobustWeight(r.Score)) / count;
            var totalIssues = records.Sum(r => r.TotalIssues);

            var stat = await _db.AiReviewUserStats
                .FirstOrDefaultAsync(s => s.Username == username && s.Repo == repo && s.StatDate == statDate);

            if (stat == null)
            {
                _db.AiReviewUserStats.Add(new AiReviewUserStat
                {
                    Username = username,
                    Repo = repo,
                    StatDate = statDate,
                    ReviewCount = count,
                    AvgScore = avgScore,
                    RobustScore = robustScore,
                    TotalIssues = totalIssues
                });
            }
            else
            {
                stat.ReviewCount = count;
                stat.AvgScore = avgScore;
                stat.RobustScore = robustScore;
                stat.TotalIssues = totalIssues;
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// 查询历史记录
        /// </summary>
        public async Task<List<ReviewHistoryResponse>> GetHistoryAsync(string? repo, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 20)
        {
            var query = _db.AiReviewRecords.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(repo))
                query = query.Where(r => r.Repo == repo);
            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt < endDate.Value.AddDays(1));

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewHistoryResponse
                {
                    Id = r.Id,
                    Repo = r.Repo,
                    PrNumber = r.PrNumber,
                    ReviewType = r.ReviewType,
                    Score = r.Score,
                    TotalIssues = r.TotalIssues,
                    Reviewer = r.Reviewer,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }

        /// <summary>
        /// 仓库排名（近 N 天）
        /// </summary>
        public async Task<List<RankingItem>> GetRepoRankingAsync(DateTime? startDate, int top = 10)
        {
            var start = startDate ?? DateTime.Now.AddDays(-30);

            var stats = await _db.AiReviewRepoStats
                .AsNoTracking()
                .Where(s => s.StatDate >= start)
                .GroupBy(s => s.Repo)
                .Select(g => new RankingItem
                {
                    Name = g.Key,
                    ReviewCount = g.Sum(s => s.ReviewCount),
                    AvgScore = g.Average(s => s.AvgScore ?? 0),
                    RobustScore = g.Sum(s => s.RobustScore * s.ReviewCount) / g.Sum(s => s.ReviewCount),
                    TotalIssues = g.Sum(s => s.TotalIssues)
                })
                .OrderByDescending(r => r.RobustScore)
                .Take(top)
                .ToListAsync();

            return stats;
        }

        /// <summary>
        /// 个人排名（近 N 天）
        /// </summary>
        public async Task<List<RankingItem>> GetUserRankingAsync(string? repo, DateTime? startDate, int top = 10)
        {
            var start = startDate ?? DateTime.Now.AddDays(-30);

            var query = _db.AiReviewUserStats
                .AsNoTracking()
                .Where(s => s.StatDate >= start);

            if (!string.IsNullOrEmpty(repo))
                query = query.Where(s => s.Repo == repo);

            var stats = await query
                .GroupBy(s => s.Username)
                .Select(g => new RankingItem
                {
                    Name = g.Key,
                    ReviewCount = g.Sum(s => s.ReviewCount),
                    AvgScore = g.Average(s => s.AvgScore ?? 0),
                    RobustScore = g.Sum(s => s.RobustScore * s.ReviewCount) / g.Sum(s => s.ReviewCount),
                    TotalIssues = g.Sum(s => s.TotalIssues)
                })
                .OrderByDescending(r => r.RobustScore)
                .Take(top)
                .ToListAsync();

            return stats;
        }

        /// <summary>
        /// 单仓库趋势图数据
        /// </summary>
        public async Task<List<RepoTrendData>> GetRepoTrendAsync(string repo, DateTime startDate, DateTime endDate)
        {
            return await _db.AiReviewRepoStats
                .AsNoTracking()
                .Where(s => s.Repo == repo && s.StatDate >= startDate && s.StatDate <= endDate)
                .OrderBy(s => s.StatDate)
                .Select(s => new RepoTrendData
                {
                    Date = s.StatDate,
                    ReviewCount = s.ReviewCount,
                    AvgScore = s.AvgScore,
                    RobustScore = s.RobustScore,
                    TotalIssues = s.TotalIssues
                })
                .ToListAsync();
        }
    }
}
