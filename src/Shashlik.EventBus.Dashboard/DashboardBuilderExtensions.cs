using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Dashboard.Areas;

namespace Shashlik.EventBus.Dashboard;

public static class DashboardBuilderExtensions
{
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

    /// <summary>
    /// 启用event bus仪表板，允许匿名访问
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="option"></param>
    /// <returns></returns>
    public static IEventBusBuilder AddDashboard(this IEventBusBuilder builder,
        EventBusDashboardOption? option = null)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<UrlService>();
        option ??= new EventBusDashboardOption();
        if (option.AuthenticateProvider is not null &&
            !typeof(IEventBusDashboardAuthentication).IsAssignableFrom(option.AuthenticateProvider))
            throw new InvalidCastException(
                $"invalid event bus authentication provider type: {option.AuthenticateProvider}");

        builder.Services.Configure<EventBusDashboardOption>(x =>
        {
            x.UrlPrefix = option.UrlPrefix;
            x.AuthenticateProvider = x.AuthenticateProvider;
        });

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
}