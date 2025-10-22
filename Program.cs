using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Markdig;
using System.Reflection.PortableExecutable;

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
            
            // 注册数据库服务
            builder.Services.AddSingleton<DatabaseService>();
            
            //// 配置应用程序监听的端口，避免端口冲突
            //builder.WebHost.ConfigureKestrel(options =>
            //{
            //    options.ListenLocalhost(5001); // 使用5001端口
            //});

            var app = builder.Build();

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

            app.Run();
        }
    }

   
}

