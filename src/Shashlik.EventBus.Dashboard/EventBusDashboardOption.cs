namespace Shashlik.EventBus.Dashboard;

/// <summary>
/// 面板选项
/// </summary>
public class EventBusDashboardOption
{
    public const string DefaultUrlPrefix = "/eventBus";

    private string _urlPrefix = DefaultUrlPrefix;

    /// <summary>
    /// 认证支持类，需要实现 <see cref="IEventBusDashboardAuthentication"/>
    /// </summary>
    public Type? AuthenticateProvider { get; set; }

    /// <summary>
    /// 面板路由前缀
    /// </summary>
    public string UrlPrefix
    {
        get => _urlPrefix;
        set
        {
            if (!value.StartsWith('/'))
            {
                _urlPrefix = '/' + value;
            }
            else
            {
                _urlPrefix = value;
            }
        }
    }
}