using System.Text.Json;using System.Text.Json.Serialization;using Microsoft.Extensions.Options;using System.Net.Http.Headers;using System.Net.Http;using System.Text;using System.Threading.Tasks;

namespace DBCheckAI
{
    public class AIConfig
    {
        public string? Provider { get; set; } = "tongyi"; // 默认使用通义千问
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
            response.EnsureSuccessStatusCode();

            // 解析响应
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TongyiResponse>(responseContent);

            return result?.output?.text?.Trim() ?? string.Empty;
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

            // 构建请求体
            var requestBody = new DeepSeekRequest
            {
                Model = _config.DeepSeekConfig.Model,
                Messages = new List<DeepSeekMessage>
                {
                    new DeepSeekMessage
                    {
                        Role = "user",
                        Content = prompt
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // 发送请求
            var response = await _httpClient.PostAsync(_config.DeepSeekConfig.BaseUrl, content);
            response.EnsureSuccessStatusCode();

            // 解析响应
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseContent);

            return result?.Choices?[0]?.Message?.Content?.Trim() ?? string.Empty;
        }
    }

    public interface IAIService
    {
        Task<string> GetResponseAsync(string prompt);
        Task<string> GetResponseAsync(string prompt, string provider);
    }
}