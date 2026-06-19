using System.Security.Cryptography;

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
    /// SecretCookieAuthenticate认证Secret值,配置<see cref="AuthenticateProvider"/>后,该值无效
    /// </summary>
    public string? AuthenticateSecret { get; set; } = "#Shashlik@.EventBus!.Secret`123";

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

    internal readonly byte[] HmacKey = RandomNumberGenerator.GetBytes(32);

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
    /// <param name="authenticateSecret">认证secret(明文,将自动转为bcrypt hash存储)</param>
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
        AuthenticateSecret = BCrypt.Net.BCrypt.HashPassword(authenticateSecret);
        if (authenticateSecretCookieName != null)
            AuthenticateSecretCookieName = authenticateSecretCookieName;
        if (authenticateSecretCookieOptions != null)
            AuthenticateSecretCookieOptions = authenticateSecretCookieOptions;
        return this;
    }


    public const string DefaultUrlPrefix = "/eventBus";
    public const string DefaultCookieName = "shashlik-eventbus-secret";
}