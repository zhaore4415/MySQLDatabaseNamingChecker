using System.Text.Json.Serialization;

namespace DBCheckAI
{
    /// <summary>
    /// 审查请求
    /// </summary>
    public class ReviewRequest
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "code"; // "code" 或 "sql"

        [JsonPropertyName("repo")]
        public string Repo { get; set; } = string.Empty;

        [JsonPropertyName("pr_number")]
        public int PrNumber { get; set; }

        [JsonPropertyName("files")]
        public List<ReviewFile> Files { get; set; } = new List<ReviewFile>();
    }

    public class ReviewFile
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("change_type")]
        public string ChangeType { get; set; } = "modified";
    }

    /// <summary>
    /// 审查响应
    /// </summary>
    public class ReviewResponse
    {
        [JsonPropertyName("score")]
        public int? Score { get; set; }

        [JsonPropertyName("total_issues")]
        public int TotalIssues { get; set; }

        [JsonPropertyName("issues")]
        public List<ReviewIssue> Issues { get; set; } = new List<ReviewIssue>();

        [JsonPropertyName("report_markdown")]
        public string ReportMarkdown { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class ReviewIssue
    {
        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("line")]
        public int? Line { get; set; }

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "warning";

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("suggestion")]
        public string Suggestion { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI 原始返回的 JSON 结构（用于解析）
    /// </summary>
    public class AIReviewResult
    {
        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("issues")]
        public List<AIReviewIssue> Issues { get; set; } = new List<AIReviewIssue>();
    }

    public class AIReviewIssue
    {
        [JsonPropertyName("file")]
        public string? File { get; set; }

        [JsonPropertyName("line")]
        public int? Line { get; set; }

        [JsonPropertyName("severity")]
        public string? Severity { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("suggestion")]
        public string? Suggestion { get; set; }
    }
}
