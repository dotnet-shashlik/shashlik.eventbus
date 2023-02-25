using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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

        if (auth != null && !await auth.AuthenticateAsync(context.HttpContext))
        {
            context.Result = RedirectToAction("Index", "Auth");
            return;
        }

        await base.OnActionExecutionAsync(context, next);
    }
}