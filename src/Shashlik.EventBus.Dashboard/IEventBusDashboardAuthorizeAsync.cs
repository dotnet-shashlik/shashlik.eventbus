using Microsoft.AspNetCore.Http;

namespace Shashlik.EventBus.Dashboard
{
    /// <summary>
    /// 异步认证用户请求
    /// </summary>
    public interface IEventBusDashboardAuthorizeAsync
    {
        /// <summary>
        /// 异步认证用户请求
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task AuthorizeAsync(HttpContext context);
    }
}
