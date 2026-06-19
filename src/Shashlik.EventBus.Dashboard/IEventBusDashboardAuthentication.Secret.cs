using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.Dashboard;

/// <summary>
/// 默认提供的基于配置的secret进行认证类
/// </summary>
public class SecretCookieAuthenticate : IEventBusDashboardAuthentication
{
    public SecretCookieAuthenticate(IOptionsMonitor<EventBusDashboardOption> options,
        ILogger<SecretCookieAuthenticate> logger)
    {
        Options = options;
        Logger = logger;
    }

    private IOptionsMonitor<EventBusDashboardOption> Options { get; }
    private ILogger<SecretCookieAuthenticate> Logger { get; }

    public Task<bool> AuthenticateAsync(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(
                Options.CurrentValue.AuthenticateSecretCookieName ?? EventBusDashboardOption.DefaultCookieName,
                out var value))
        {
            if (string.IsNullOrWhiteSpace(value))
                return Task.FromResult(false);
            try
            {
                var parts = value.Split('.');
                if (parts.Length != 2)
                    return Task.FromResult(false);

                var tokenBytes = Convert.FromBase64String(parts[0]);
                var signatureBytes = Convert.FromBase64String(parts[1]);

                using var hmac = new HMACSHA256(Options.CurrentValue.HmacKey);
                var computed = hmac.ComputeHash(tokenBytes);
                if (!CryptographicOperations.FixedTimeEquals(computed, signatureBytes))
                    return Task.FromResult(false);

                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                Logger.LogDebug(e, "invalid cookie secret.");
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(false);
    }
}