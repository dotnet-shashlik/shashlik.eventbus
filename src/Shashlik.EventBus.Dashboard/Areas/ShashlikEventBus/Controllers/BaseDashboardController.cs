using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Controllers;

[AllowAnonymous]
[Area(Consts.AreaName)]
public abstract class BaseDashboardController : Controller
{
    protected BaseDashboardController(IOptionsMonitor<EventBusDashboardOption> options)
    {
        Options = options;
    }

    protected IOptionsMonitor<EventBusDashboardOption> Options { get; }

    [ViewData] public string UrlPrefix => Options.CurrentValue.UrlPrefix;

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var auth = context.HttpContext.RequestServices.GetService<IEventBusDashboardAuthentication>();

        // 未配置任何 IEventBusDashboardAuthentication 时,默认拒绝访问。
        // 之前的实现是 "auth == null 直接放行",会把发布/重试接口暴露给任何能访问 URL 的人。
        if (auth is null)
        {
            var logger = context.HttpContext.RequestServices
                .GetService<ILoggerFactory>()
                ?.CreateLogger("Shashlik.EventBus.Dashboard");
            logger?.LogError(
                "[EventBus-Dashboard] No IEventBusDashboardAuthentication registered. Refusing all dashboard requests to avoid exposing the publish/retry APIs without auth. Register one via 'AddDashboard(opt => opt.UseSecretAuthenticate(secret))' or 'AddDashboard<TAuth>()'.");

            context.Result = new ObjectResult("Dashboard authentication is not configured.")
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        if (!await auth.AuthenticateAsync(context.HttpContext))
        {
            context.Result = RedirectToAction("Index", "Auth");
            return;
        }

        await base.OnActionExecutionAsync(context, next);
    }
}