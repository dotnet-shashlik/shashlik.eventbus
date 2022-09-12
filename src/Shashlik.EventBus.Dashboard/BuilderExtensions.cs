using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Dashboard.Areas;

namespace Shashlik.EventBus.Dashboard
{
    public static class BuilderExtensions
    {
        public static IApplicationBuilder UseEventBusDashboard(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetService<IOptions<EventBusDashboardOption>>();
            var option = options!.Value;
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new EmbeddedFileProvider(typeof(BuilderExtensions).Assembly, "Shashlik.EventBus.Dashboard.Resource"),
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

        public static IEventBusBuilder AddShashlikDashboard(this IEventBusBuilder builder, EventBusDashboardOption? option = null)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<UrlService>();
            option ??= new EventBusDashboardOption();

            builder.Services.Configure<EventBusDashboardOption>(x =>
            {
                x.UrlPrefix = option.UrlPrefix;
            });
            builder.Services.AddMvc().AddApplicationPart(typeof(BuilderExtensions).Assembly);
            return builder;
        }

        public static IEventBusBuilder AddShashlikDashboard<TAuth>(this IEventBusBuilder builder)
            where TAuth : class, IEventBusDashboardAuthorize
        {
            builder.Services.AddScoped<IEventBusDashboardAuthorize, TAuth>();
            return builder.AddShashlikDashboard();
        }

        public static IEventBusBuilder AddShashlikDashboardAuthAsync<TAuthAsync>(this IEventBusBuilder builder)
            where TAuthAsync : class, IEventBusDashboardAuthorizeAsync
        {
            builder.Services.AddScoped<IEventBusDashboardAuthorizeAsync, TAuthAsync>();
            return builder.AddShashlikDashboard();
        }
    }
}
