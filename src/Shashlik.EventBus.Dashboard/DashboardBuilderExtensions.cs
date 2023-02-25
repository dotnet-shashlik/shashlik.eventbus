using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Dashboard.Areas;

namespace Shashlik.EventBus.Dashboard;

public static class DashboardBuilderExtensions
{
    /// <summary>
    /// 启用event bus仪表板
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="option"></param>
    /// <returns></returns>
    public static IEventBusBuilder AddDashboard(this IEventBusBuilder builder,
        EventBusDashboardOption? option = null)
    {
        if (option == null)
        {
            builder.AddDashboard(_ => { });
        }
        else
        {
            builder.AddDashboard(options =>
            {
                options.UrlPrefix = option.UrlPrefix;
                options.AuthenticateProvider = option.AuthenticateProvider;
                options.AuthenticateSecret = option.AuthenticateSecret;
                options.AuthenticateSecretCookieName = option.AuthenticateSecretCookieName;
                options.AuthenticateSecretCookieOptions = option.AuthenticateSecretCookieOptions;
            });
        }

        return builder;
    }

    /// <summary>
    /// 启用event bus仪表板
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="optionAction"></param>
    /// <returns></returns>
    public static IEventBusBuilder AddDashboard(this IEventBusBuilder builder,
        Action<EventBusDashboardOption> optionAction)
    {
        builder.Services.Configure(optionAction);
        var option = new EventBusDashboardOption();
        optionAction(option);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<UrlService>();
        if (option.AuthenticateProvider != null &&
            option.AuthenticateProvider == typeof(SecretCookieAuthenticate))
        {
            // 使用DataProtection来加密数据并存储到cookie
            builder.Services.AddDataProtection();
            if (string.IsNullOrWhiteSpace(option.AuthenticateSecret))
                throw new ArgumentException("AuthenticateSecret can not be empty.");
        }

        if (option.AuthenticateProvider is not null &&
            !typeof(IEventBusDashboardAuthentication).IsAssignableFrom(option.AuthenticateProvider))
            throw new InvalidCastException(
                $"invalid event bus authentication provider type: {option.AuthenticateProvider}");

        if (option.AuthenticateProvider is not null)
            builder.Services.AddSingleton(typeof(IEventBusDashboardAuthentication), option.AuthenticateProvider);

        builder.Services.AddMvc().AddApplicationPart(typeof(DashboardBuilderExtensions).Assembly);
        return builder;
    }

    /// <summary>
    /// 启用event bus仪表板， 使用认证类<typeparamref name="TAuth"/>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="prefix"></param>
    /// <typeparam name="TAuth">认证类型</typeparam>
    /// <returns></returns>
    public static IEventBusBuilder AddDashboard<TAuth>(this IEventBusBuilder builder, string? prefix = null)
        where TAuth : class, IEventBusDashboardAuthentication
    {
        return builder.AddDashboard(new EventBusDashboardOption
        {
            UrlPrefix = prefix ?? EventBusDashboardOption.DefaultUrlPrefix,
            AuthenticateProvider = typeof(TAuth)
        });
    }

    /// <summary>
    /// 启用event bus仪表板
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseEventBusDashboard(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<IOptions<EventBusDashboardOption>>();
        var option = options!.Value;
        app.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = new EmbeddedFileProvider(typeof(DashboardBuilderExtensions).Assembly,
                "Shashlik.EventBus.Dashboard.Resource"),
            RequestPath = option.UrlPrefix + "/static"
        });
        app.UseEndpoints(e =>
        {
            e.MapAreaControllerRoute(name: Consts.AreaName,
                areaName: Consts.AreaName,
                pattern: option.UrlPrefix + "/{controller=Published}/{action=Index}/{id?}");
        });
        return app;
    }
}