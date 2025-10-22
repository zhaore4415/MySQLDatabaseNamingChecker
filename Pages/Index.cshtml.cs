using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Markdig;

namespace DBCheckAI.Pages
{
    public class IndexModel : PageModel
    {
        private readonly DatabaseService _databaseService;

        public IndexModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            // 不再写死连接字符串，让用户从前端输入输入
            ConnectionString = string.Empty;
            // 设置默认命名规则
            DefaultNamingRules = GetDefaultNamingRules();
            // 初始化NamingRules为默认规则
            NamingRules = DefaultNamingRules;
            // 初始化AIProviderOptions
            AIProviderOptions = new List<SelectListItem>();
            // 初始化报告属性
            ReportMarkdown = string.Empty;
            ReportHtml = string.Empty;
        }

        [BindProperty]
        public string ConnectionString { get; set; }

        [BindProperty]
        public string NamingRules { get; set; }

        public string DefaultNamingRules { get; set; }
        public string ReportMarkdown { get; set; }
        public string ReportHtml { get; set; }

        [BindProperty]
        public string AIProvider { get; set; } = "simulation"; // 默认使用模拟AI

        public List<SelectListItem> AIProviderOptions { get; set; }

        public void OnGet()
        {            // 初始化页面数据
            // 如果是首次访问，设置默认的连接字符串占位符
            if (string.IsNullOrEmpty(ConnectionString))
            {
                ConnectionString = "Server=localhost;Database=test;Uid=root;Pwd=password;";
            }

            // 初始化AI提供商选项
            AIProviderOptions = new List<SelectListItem>
            {                new SelectListItem { Value = "simulation", Text = "模拟AI (无需API密钥)" },
                new SelectListItem { Value = "tongyi", Text = "通义千问" },
                new SelectListItem { Value = "deepseek", Text = "DeepSeek" }
            };
        }

        // 处理传统表单提交
        public async Task<IActionResult> OnPostAsync()
        {
            // 确保AIProviderOptions不为null，无论ModelState是否有效
            if (AIProviderOptions == null || AIProviderOptions.Count == 0)
            {
                AIProviderOptions = new List<SelectListItem>
                {                    new SelectListItem { Value = "simulation", Text = "模拟AI (无需API密钥)" },
                    new SelectListItem { Value = "tongyi", Text = "通义千问" },
                    new SelectListItem { Value = "deepseek", Text = "DeepSeek" }
                };
            }
            
            // 验证表单数据
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // 确保NamingRules有值
                if (string.IsNullOrEmpty(NamingRules))
                {
                    NamingRules = DefaultNamingRules;
                }

                // 提取数据库架构
                var schema = await _databaseService.GetMySQLDatabaseSchemaAsync(ConnectionString);

                // 检查命名规则，传入AI提供商选项
                ReportMarkdown = await _databaseService.CheckNamingWithRulesAsync(schema, NamingRules, AIProvider);

                // 转换Markdown为HTML
                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();
                ReportHtml = Markdown.ToHtml(ReportMarkdown, pipeline);

                // 重新初始化AI提供商选项，确保表单提交后不会丢失
                AIProviderOptions = new List<SelectListItem>
                {                    new SelectListItem { Value = "simulation", Text = "模拟AI (无需API密钥)" },
                    new SelectListItem { Value = "tongyi", Text = "通义千问" },
                    new SelectListItem { Value = "deepseek", Text = "DeepSeek" }
                };
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"检查过程中出现错误: {ex.Message}");
            }
            
            // 确保AIProviderOptions不为null
            if (AIProviderOptions == null)
            {
                AIProviderOptions = new List<SelectListItem>();
            }
            
            // 确保AIProvider有默认值
            if (string.IsNullOrEmpty(AIProvider))
            {
                AIProvider = "simulation";
            }

            return Page();
        }
        
        // 处理AJAX请求，返回JSON响应
        public async Task<JsonResult> OnPostJsonAsync()
        {
            try
            {
                // 确保NamingRules有值
                if (string.IsNullOrEmpty(NamingRules))
                {
                    NamingRules = DefaultNamingRules;
                }

                // 提取数据库架构
                var schema = await _databaseService.GetMySQLDatabaseSchemaAsync(ConnectionString);

                // 检查命名规则，传入AI提供商选项
                ReportMarkdown = await _databaseService.CheckNamingWithRulesAsync(schema, NamingRules, AIProvider);

                // 转换Markdown为HTML
                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();
                ReportHtml = Markdown.ToHtml(ReportMarkdown, pipeline);

                // 返回成功的JSON响应
                return new JsonResult(new {
                    success = true,
                    reportHtml = ReportHtml
                });
            }
            catch (Exception ex)
            {
                // 返回错误的JSON响应
                return new JsonResult(new {
                    success = false,
                    error = $"检查过程中出现错误: {ex.Message}"
                });
            }
        }

        private string GetDefaultNamingRules()
        { return @"## MySQL数据库命名规范

✅ 表名规范：
- 使用复数名词，如 users, orders
- 使用小写 + 下划线：user_profile, order_item
- 不允许使用大写字母或驼峰
- 长度不超过64字符

✅ 字段名规范：
- 使用小写下划线命名法：user_id, create_time, total_amount
- 主键必须叫 id
- 外键必须以 _id 结尾，如 user_id, order_id
- 创建时间字段叫 create_time，更新时间叫 update_time
- 布尔字段用 is_xxx，如 is_active, is_deleted

❌ 禁止：
- 使用拼音（如 yonghu）
- 使用MySQL保留字（如 order, user, group 等）
- 使用空格或特殊字符
- 使用MySQL不支持的字符

✅ 索引命名规范：
- 主键：PRIMARY KEY (id)
- 普通索引：idx_字段名，如 idx_user_id
- 唯一索引：uk_字段名，如 uk_email
- 复合索引：idx_字段1_字段2，如 idx_user_id_status"; }
    }
}