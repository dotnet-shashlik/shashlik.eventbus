namespace Shashlik.EventBus.Dashboard;

/// <summary>
/// EventBus认证用户请求
/// </summary>
public interface IEventBusDashboardAuthentication
{
    /// <summary>
    /// 认证用户请求
    /// </summary>
    /// <param name="context">http context</param>
    /// <returns>认证结果</returns>
    Task<bool> AuthenticateAsync(HttpContext context);
}