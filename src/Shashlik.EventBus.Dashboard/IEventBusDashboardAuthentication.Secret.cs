using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.Dashboard;

/// <summary>
/// 默认提供的基于配置的secret进行认证类
/// </summary>
public class SecretCookieAuthenticate : IEventBusDashboardAuthentication
{
    public SecretCookieAuthenticate(IOptionsMonitor<EventBusDashboardOption> options,
        IDataProtectionProvider dataProtectionProvider, ILogger<SecretCookieAuthenticate> logger)
    {
        Options = options;
        DataProtectionProvider = dataProtectionProvider;
        Logger = logger;
    }

    private IOptionsMonitor<EventBusDashboardOption> Options { get; }
    private IDataProtectionProvider DataProtectionProvider { get; }
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
                var unprotect = DataProtectionProvider.CreateProtector(EventBusDashboardOption.DataProtectorName)
                    .Unprotect(value);
                return Task.FromResult(unprotect == Options.CurrentValue.AuthenticateSecret);
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