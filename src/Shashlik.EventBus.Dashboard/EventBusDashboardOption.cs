using System.Text.RegularExpressions;

namespace Shashlik.EventBus.Dashboard;

/// <summary>
/// 面板选项
/// </summary>
public class EventBusDashboardOption
{
    /// <summary>
    /// <see cref="AuthenticateSecret"/> 允许的字符集(英文/数字/常见密码特殊符号),且长度必须为 32 字节
    /// </summary>
    public const string AuthenticateSecretPattern = @"^[A-Za-z0-9!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?`~]+$";

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
    /// SecretCookieAuthenticate认证Secret值,明文存储,仅支持英文/数字/常见密码特殊符号,长度必须为 32 字节
    /// 配置<see cref="AuthenticateProvider"/>后,该值无效
    /// </summary>
    public string? AuthenticateSecret { get; set; } = "ShashlikEventBus.DashboardKey#32";

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
    /// <param name="authenticateSecret">认证secret,明文存储,必须为 32 字符,且仅支持英文/数字/常见密码特殊符号</param>
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
        ValidateAuthenticateSecret(authenticateSecret);
        UseAuthenticate<SecretCookieAuthenticate>();
        AuthenticateSecret = authenticateSecret;
        if (authenticateSecretCookieName != null)
            AuthenticateSecretCookieName = authenticateSecretCookieName;
        if (authenticateSecretCookieOptions != null)
            AuthenticateSecretCookieOptions = authenticateSecretCookieOptions;
        return this;
    }

    /// <summary>
    /// 校验 <see cref="AuthenticateSecret"/> 格式:长度必须为 32,且仅允许英文/数字/常见密码特殊符号
    /// </summary>
    /// <param name="value"></param>
    /// <exception cref="ArgumentException"></exception>
    public static void ValidateAuthenticateSecret(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("AuthenticateSecret can not be null or empty.", nameof(value));
        if (value.Length != 32)
            throw new ArgumentException(
                $"AuthenticateSecret length must be 32 bytes (current: {value.Length}).",
                nameof(value));
        if (!Regex.IsMatch(value, AuthenticateSecretPattern))
            throw new ArgumentException(
                "AuthenticateSecret contains invalid characters. Only English letters, digits and common password special characters are allowed: !@#$%^&*()_+-=[]{};':\"\\|,.<>/?`~",
                nameof(value));
    }


    public const string DefaultUrlPrefix = "/eventbus";
    public const string DefaultCookieName = "shashlik-eventbus-secret";
}