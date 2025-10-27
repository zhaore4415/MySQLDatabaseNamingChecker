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

1. 表名和列名小写加下划线，如: subject_category表，parent_id列

2. 所有字段不可为空，如：
   - parent_id等id类默认值为0
   - 字符串类默认值为空字符串("")
   - 日期时间类默认值为1970-1

3. 其他类型视业务场景取一个安全值作为默认值，如余额为null时默认值是0

4. 所有表需要完整审计属性，即创建人、创建时间、更新人、更新时间、软删除

5. 除主键和唯一索引外，不可添加其他任何约束，唯一索引尽量只添加一组

6. 唯一索引最好只包括一个列，如单列实在无法保证唯一性，最多只允许三个字段

7. 唯一索引的字段不允许为null，null值会加大唯一性检查的复杂度，会进一步降低性能

8. 审计属性规范：
   # 新版审计属性
   created_by
   created_at
   updated_by
   updated_at
   deleted_by
   deleted_at
   is_deleted

   # dotnet老的审计属性
   create_user_id
   create_time
   update_user_id
   update_time
   is_deleted

9. 需要数据清洗的表，添加一个last_time字段，不会与update_time冲突

10. 唯一索引需要包含软删除字段(is_deleted)，防止新增冲突

11. 检查表名和字段名的单词是否有拼写错误

12. 检查哪些表少了新版审计属性，如deleted_by、deleted_at"; }
    }
}