using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Dashboard.Areas;

namespace Shashlik.EventBus.Dashboard;

public static class DashboardBuilderExtensions
{
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

        builder.Services.Configure<EventBusDashboardOption>(x => { x.UrlPrefix = option.UrlPrefix; });
        builder.Services.AddMvc().AddApplicationPart(typeof(DashboardBuilderExtensions).Assembly);
        return builder;
    }

    /// <summary>
    /// 启用event bus仪表板，使用自定义认证
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="TAuth"></typeparam>
    /// <returns></returns>
    public static IEventBusBuilder AddDashboard<TAuth>(this IEventBusBuilder builder)
        where TAuth : class, IEventBusDashboardAuthentication
    {
        builder.Services.AddScoped<IEventBusDashboardAuthentication, TAuth>();
        return builder.AddDashboard();
    }
}