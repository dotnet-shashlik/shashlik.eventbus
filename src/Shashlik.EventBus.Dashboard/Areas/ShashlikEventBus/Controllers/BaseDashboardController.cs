using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Controllers;

[Area(Consts.AreaName)]
public abstract class BaseDashboardController : Controller
{
    [ViewData]
    public string UrlPrefix =>
        HttpContext.RequestServices.GetService<IOptions<EventBusDashboardOption>>()!.Value.UrlPrefix;

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var auth = context.HttpContext.RequestServices.GetService<IEventBusDashboardAuthentication>();

        if (auth != null && !await auth.AuthenticateAsync(context.HttpContext))
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Result = Content("Unauthorized");

            return;
        }

        await base.OnActionExecutionAsync(context, next);
    }
}