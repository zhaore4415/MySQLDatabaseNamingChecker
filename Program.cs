using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DBCheckAI.Data;
using DBCheckAI.Services;

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
            
            // 注册数据库服务（现有MySQL检查服务）
            builder.Services.AddSingleton<DatabaseService>();
            
            // 注册 SQLite 数据库上下文（AI 审查历史记录）
            var dbPath = builder.Configuration.GetValue("Database:Path", "review.db");
            builder.Services.AddDbContext<ReviewDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));
            
            // 注册 AI 审查报告服务
            builder.Services.AddScoped<IReviewReportService, ReviewReportService>();
            
            //// 配置应用程序监听的端口，避免端口冲突
            //builder.WebHost.ConfigureKestrel(options =>
            //{
            //    options.ListenLocalhost(5001); // 使用5001端口
            //});

            var app = builder.Build();

            // 自动迁移 SQLite 数据库（首次启动时创建表）
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
                db.Database.Migrate();
            }

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

            // 注册 API 审查接口 + 上报接口
            app.MapReviewApis();

            app.Run();
        }
    }
}


