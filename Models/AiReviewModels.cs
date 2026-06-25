using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBCheckAI.Models
{
    /// <summary>
    /// AI 审查记录主表
    /// </summary>
    public class AiReviewRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>仓库全名，如 Dotnet/SJZY.CAMS.Api</summary>
        [Required]
        [StringLength(255)]
        public string Repo { get; set; } = string.Empty;

        /// <summary>PR 编号</summary>
        public int PrNumber { get; set; }

        /// <summary>源分支</summary>
        [StringLength(100)]
        public string SourceBranch { get; set; } = string.Empty;

        /// <summary>目标分支</summary>
        [StringLength(100)]
        public string TargetBranch { get; set; } = string.Empty;

        /// <summary>触发审查的 commit SHA</summary>
        [StringLength(40)]
        public string CommitSha { get; set; } = string.Empty;

        /// <summary>审查类型：code 或 sql</summary>
        [StringLength(10)]
        public string ReviewType { get; set; } = "code";

        /// <summary>AI 评分 0-100</summary>
        public int? Score { get; set; }

        /// <summary>发现问题总数</summary>
        public int TotalIssues { get; set; }

        /// <summary>AI 生成的 Markdown 报告</summary>
        public string ReportMarkdown { get; set; } = string.Empty;

        /// <summary>使用的 AI 模型</summary>
        [StringLength(50)]
        public string AiModel { get; set; } = string.Empty;

        /// <summary>消耗的 Token 数</summary>
        public int? TokenUsed { get; set; }

        /// <summary>审查耗时（毫秒）</summary>
        public int? DurationMs { get; set; }

        /// <summary>提交者用户名</summary>
        [StringLength(100)]
        public string Reviewer { get; set; } = string.Empty;

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>关联的问题明细</summary>
        public List<AiReviewIssue> Issues { get; set; } = new();
    }

    /// <summary>
    /// AI 审查问题明细表
    /// </summary>
    public class AiReviewIssue
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>关联的审查记录 ID</summary>
        public long ReviewId { get; set; }

        /// <summary>问题所在文件</summary>
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>问题行号</summary>
        public int? LineNumber { get; set; }

        /// <summary>严重程度：error, warning, info</summary>
        [StringLength(10)]
        public string Severity { get; set; } = "warning";

        /// <summary>问题分类</summary>
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        /// <summary>问题描述</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>改进建议</summary>
        public string Suggestion { get; set; } = string.Empty;

        /// <summary>导航属性</summary>
        [ForeignKey("ReviewId")]
        public AiReviewRecord Review { get; set; } = null!;
    }

    /// <summary>
    /// 仓库统计汇总表（按日）
    /// </summary>
    public class AiReviewRepoStat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Repo { get; set; } = string.Empty;

        public DateTime StatDate { get; set; }

        public int ReviewCount { get; set; }

        /// <summary>简单平均分</summary>
        public decimal? AvgScore { get; set; }

        /// <summary>加权健壮性分</summary>
        public decimal? RobustScore { get; set; }

        public int TotalIssues { get; set; }
    }

    /// <summary>
    /// 个人统计汇总表（按日）
    /// </summary>
    public class AiReviewUserStat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Repo { get; set; } = string.Empty;

        public DateTime StatDate { get; set; }

        public int ReviewCount { get; set; }

        public decimal? AvgScore { get; set; }

        public decimal? RobustScore { get; set; }

        public int TotalIssues { get; set; }
    }
}
