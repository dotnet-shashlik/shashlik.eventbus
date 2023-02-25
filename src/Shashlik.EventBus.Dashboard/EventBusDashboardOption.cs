namespace Shashlik.EventBus.Dashboard;

/// <summary>
/// 面板选项
/// </summary>
public class EventBusDashboardOption
{
    private string _urlPrefix = DefaultUrlPrefix;

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

    /// <summary>
    /// 认证支持类，需要实现 <see cref="IEventBusDashboardAuthentication"/>, 如果无需认证,该值设置为null即可
    /// </summary>
    public Type? AuthenticateProvider { get; set; }

    /// <summary>
    /// SecretCookieAuthenticate认证Secret值
    /// <para></para>
    /// 默认每次都生成一个guid
    /// </summary>
    public string? AuthenticateSecret { get; set; }

    /// <summary>
    /// SecretCookieAuthenticate认证,cookie名称
    /// </summary>
    public string? AuthenticateSecretCookieName { get; set; } = DefaultCookieName;

    /// <summary>
    /// SecretCookieAuthenticate认证,cookie设置,默认2小时过期
    /// </summary>
    public Func<HttpContext, CookieOptions>? AuthenticateSecretCookieOptions { get; set; } = _ => new CookieOptions
    {
        Expires = DateTimeOffset.Now.AddHours(2),
    };

    /// <summary>
    /// 使用指定类型<typeparamref name="T"/>作为认证类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public EventBusDashboardOption UseAuthenticate<T>()
        where T : IEventBusDashboardAuthentication
    {
        AuthenticateProvider = typeof(T);
        return this;
    }

    /// <summary>
    /// 使用<see cref="SecretCookieAuthenticate"/>作为认证类
    /// </summary>
    /// <param name="authenticateSecret">认证secret</param>
    /// <param name="authenticateSecretCookieName">认证cookie name</param>
    /// <param name="authenticateSecretCookieOptions">认证cookie 配置</param>
    /// <returns></returns>
    public EventBusDashboardOption UseSecretAuthenticate(
        string authenticateSecret,
        string? authenticateSecretCookieName = null,
        Func<HttpContext, CookieOptions>? authenticateSecretCookieOptions = null)
    {
        if (string.IsNullOrWhiteSpace(authenticateSecret))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(authenticateSecret));
        UseAuthenticate<SecretCookieAuthenticate>();
        AuthenticateSecret = authenticateSecret;
        if (AuthenticateSecretCookieName != null)
            AuthenticateSecretCookieName = authenticateSecretCookieName;
        if (AuthenticateSecretCookieOptions != null)
            AuthenticateSecretCookieOptions = authenticateSecretCookieOptions;
        return this;
    }


    public const string DefaultUrlPrefix = "/eventBus";
    public const string DataProtectorName = "Shashlik.EventBus.Dashboard";
    public const string DefaultCookieName = "shashlik-eventbus-secret";
}