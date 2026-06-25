using System.ComponentModel.DataAnnotations;

namespace DBCheckAI.Models
{
    /// <summary>
    /// AI 审查结果上报请求 DTO
    /// </summary>
    public class ReviewReportRequest
    {
        [Required]
        [StringLength(255)]
        public string Repo { get; set; } = string.Empty;

        [Required]
        public int PrNumber { get; set; }

        [StringLength(100)]
        public string SourceBranch { get; set; } = string.Empty;

        [StringLength(100)]
        public string TargetBranch { get; set; } = string.Empty;

        [StringLength(40)]
        public string CommitSha { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string ReviewType { get; set; } = "code";

        public int? Score { get; set; }
        public int TotalIssues { get; set; }
        public string ReportMarkdown { get; set; } = string.Empty;
        [StringLength(50)]
        public string AiModel { get; set; } = string.Empty;
        public int? TokenUsed { get; set; }
        public int? DurationMs { get; set; }
        [StringLength(100)]
        public string Reviewer { get; set; } = string.Empty;

        public List<ReviewIssueDto> Issues { get; set; } = new();
    }

    public class ReviewIssueDto
    {
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;
        public int? LineNumber { get; set; }
        [StringLength(10)]
        public string Severity { get; set; } = "warning";
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
    }

    /// <summary>
    /// 审查历史查询响应
    /// </summary>
    public class ReviewHistoryResponse
    {
        public long Id { get; set; }
        public string Repo { get; set; } = string.Empty;
        public int PrNumber { get; set; }
        public string ReviewType { get; set; } = string.Empty;
        public int? Score { get; set; }
        public int TotalIssues { get; set; }
        public string Reviewer { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 仓库/个人排名项
    /// </summary>
    public class RankingItem
    {
        public string Name { get; set; } = string.Empty;
        public int ReviewCount { get; set; }
        public decimal? AvgScore { get; set; }
        public decimal? RobustScore { get; set; }
        public int TotalIssues { get; set; }
    }

    /// <summary>
    /// 仓库趋势数据
    /// </summary>
    public class RepoTrendData
    {
        public DateTime Date { get; set; }
        public int ReviewCount { get; set; }
        public decimal? AvgScore { get; set; }
        public decimal? RobustScore { get; set; }
        public int TotalIssues { get; set; }
    }
}
