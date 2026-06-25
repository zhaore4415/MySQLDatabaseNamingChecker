using Microsoft.AspNetCore.Builder;

namespace DBCheckAI
{
    /// <summary>
    /// API 注册扩展方法
    /// </summary>
    public static class ApiExtensions
    {
        /// <summary>
        /// 一键注册所有 AI 审查 API 接口
        /// </summary>
        public static void MapReviewApis(this WebApplication app)
        {
            ReviewApi.Map(app);
            ReviewReportApi.Map(app);
        }
    }
}
