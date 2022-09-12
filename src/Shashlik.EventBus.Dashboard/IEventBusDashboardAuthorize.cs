using Microsoft.AspNetCore.Http;

namespace Shashlik.EventBus.Dashboard
{
    /// <summary>
    /// 认证用户请求
    /// </summary>
    public interface IEventBusDashboardAuthorize
    {
        /// <summary>
        /// 认证用户请求
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        void Authorize(HttpContext context);
    }
}
