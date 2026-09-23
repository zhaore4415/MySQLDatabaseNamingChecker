using System.Text.Json;using System.Text.Json.Serialization;using Microsoft.Extensions.Options;using System.Net.Http.Headers;using System.Net.Http;using System.Text;using System.Threading.Tasks;using System.Collections.Generic;

namespace DBCheckAI
{
    public class AIConfig
    {
        public string? Provider { get; set; } = "tongyi"; // 默认使用通义千问
        public string? ApiToken { get; set; } = string.Empty; // 外部 API 调用鉴权令牌
        public TongyiConfig? TongyiConfig { get; set; } = new TongyiConfig();
        public DeepSeekConfig? DeepSeekConfig { get; set; } = new DeepSeekConfig();
    }

    public class TongyiConfig
    {
        public string? ApiKey { get; set; }
        public string? Model { get; set; } = "qwen-turbo";
        public string? BaseUrl { get; set; } = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";
    }

    public class DeepSeekConfig
    {
        public string? ApiKey { get; set; }
        public string? Model { get; set; } = "deepseek-chat";
        public string? BaseUrl { get; set; } = "https://api.deepseek.com/v1/chat/completions";
    }

    // 通义千问API响应模型
    public class TongyiResponse
    {
        public TongyiOutput? output { get; set; }
        public TongyiUsage? usage { get; set; }
        public string? request_id { get; set; }
    }

    public class TongyiOutput
    {
        public string? text { get; set; }
        public string? finish_reason { get; set; }
    }

    public class TongyiUsage
    {
        public int? total_tokens { get; set; }
        public int? input_tokens { get; set; }
        public int? output_tokens { get; set; }
    }

    // DeepSeek API请求模型
    public class DeepSeekMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    public class DeepSeekRequest
    {
        public string? Model { get; set; }
        public List<DeepSeekMessage>? Messages { get; set; }
    }

    // DeepSeek API响应模型
    public class DeepSeekResponse
    {
        public List<DeepSeekChoice>? Choices { get; set; }
        public TongyiUsage? Usage { get; set; }
    }

    public class DeepSeekChoice
    {
        public DeepSeekMessage? Message { get; set; }
    }

    // ===== 命名规范检查结果模型（独立于 ReviewModels.cs 的代码审查模型） =====
    public class NamingReviewResult
    {
        [JsonPropertyName("summary")]
        public NamingReviewSummary? Summary { get; set; }
        [JsonPropertyName("issues")]
        public List<NamingReviewIssue>? Issues { get; set; }
    }

    public class NamingReviewSummary
    {
        [JsonPropertyName("total_tables")]
        public int TotalTables { get; set; }
        [JsonPropertyName("total_columns")]
        public int TotalColumns { get; set; }
        [JsonPropertyName("issues_count")]
        public int IssuesCount { get; set; }
        [JsonPropertyName("compliance_rate")]
        public double ComplianceRate { get; set; }
    }

    public class NamingReviewIssue
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("object")]
        public string? Object { get; set; }
        [JsonPropertyName("current_name")]
        public string? CurrentName { get; set; }
        [JsonPropertyName("problem")]
        public string? Problem { get; set; }
        [JsonPropertyName("suggestion")]
        public string? Suggestion { get; set; }
    }
    // ===== 结束命名规范检查结果模型 =====

    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly AIConfig _config;

        public AIService(HttpClient httpClient, IOptions<AIConfig> config)
        {
            _httpClient = httpClient;
            _config = config.Value;
        }

        public async Task<string> GetResponseAsync(string prompt)
        {
            // 使用配置中的默认提供商
            return await GetResponseAsync(prompt, _config.Provider);
        }
        
        /// <summary>
        /// 根据指定的提供商获取AI响应
        /// </summary>
        /// <param name="prompt">提示词</param>
        /// <param name="provider">提供商：tongyi、deepseek</param>
        /// <returns>AI生成的响应文本</returns>
        public async Task<string> GetResponseAsync(string prompt, string provider)
        {
            switch (provider?.ToLower() ?? _config.Provider.ToLower())
            {
                case "deepseek":
                    return await CallDeepSeekAsync(prompt);
                case "tongyi":
                    return await CallTongyiAsync(prompt);
                default:
                    // 如果是未知提供商，使用默认配置
                    if (_config.Provider.ToLower() == "deepseek")
                        return await CallDeepSeekAsync(prompt);
                    else
                        return await CallTongyiAsync(prompt);
            }
        }

        private async Task<string> CallTongyiAsync(string prompt)
        {
            if (string.IsNullOrEmpty(_config.TongyiConfig.ApiKey))
            {
                throw new Exception("通义千问API密钥未配置");
            }

            // 设置请求头
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.TongyiConfig.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 构建请求体
            var requestBody = new
            {
                model = _config.TongyiConfig.Model,
                input = new { prompt = prompt },
                parameters = new
                {
                    temperature = 0.01,
                    top_p = 0.9,
                    max_tokens = 2000
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // 发送请求
            var response = await _httpClient.PostAsync(_config.TongyiConfig.BaseUrl, content);

            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"通义千问 API 调用失败: {response.StatusCode} - {responseContent}");
            }

            // 解析响应
            var result = JsonSerializer.Deserialize<TongyiResponse>(responseContent);

            var outputText = result?.output?.text?.Trim();
            return outputText ?? string.Empty;
        }

        private async Task<string> CallDeepSeekAsync(string prompt)
        {
            if (string.IsNullOrEmpty(_config.DeepSeekConfig.ApiKey))
            {
                throw new Exception("DeepSeek API密钥未配置");
            }

            // 设置请求头
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.DeepSeekConfig.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 构建请求体，使用匿名类型确保正确的JSON序列化
            var requestBody = new {
                model = _config.DeepSeekConfig.Model,
                temperature = 0,
                seed = 20240601,
                messages = new List<object> {
                    new {
                        role = "user",
                        content = prompt
                    }
                }
            };

            // 添加try-catch以处理API错误
            try {
                // 使用JsonSerializerOptions确保正确序列化
                var options = new JsonSerializerOptions {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                
                var jsonContent = JsonSerializer.Serialize(requestBody, options);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 发送请求
                var response = await _httpClient.PostAsync(_config.DeepSeekConfig.BaseUrl, content);
                
                // 检查响应状态
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"DeepSeek API调用失败: {response.StatusCode} - {errorContent}");
                }

                // 解析响应
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseContent, options);

                if (result?.Choices == null || result.Choices.Count == 0 || result.Choices[0]?.Message?.Content == null)
                {
                    throw new Exception("DeepSeek API返回了空响应或格式不正确");
                }

                return result.Choices[0].Message.Content.Trim();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"网络请求异常: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new Exception($"JSON解析错误: {ex.Message}");
            }
        }
    }

    public interface IAIService
    {
        Task<string> GetResponseAsync(string prompt);
        Task<string> GetResponseAsync(string prompt, string provider);
    }
}