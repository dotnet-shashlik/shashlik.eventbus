using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Controllers
{
    [Area(Consts.AreaName)]
    public abstract class BaseDashboardController : Controller
    {
        [ViewData]
        public string UrlPrefix => HttpContext.RequestServices.GetService<IOptions<EventBusDashboardOption>>()!.Value.UrlPrefix;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var auth = context.HttpContext.RequestServices.GetService<IEventBusDashboardAuthorize>();
            if (auth != null)
            {
                auth.Authorize(context.HttpContext);
                if (context.HttpContext.User.Identity == null || !context.HttpContext.User.Identity.IsAuthenticated)
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Result = NoContent();
                }

                return;
            }
            base.OnActionExecuting(context);
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var auth = context.HttpContext.RequestServices.GetService<IEventBusDashboardAuthorizeAsync>();
            if (auth != null)
            {
                await auth.AuthorizeAsync(context.HttpContext);
                if (context.HttpContext.User.Identity == null || !context.HttpContext.User.Identity.IsAuthenticated)
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Result = NoContent();
                }

                return;
            }
            await base.OnActionExecutionAsync(context, next);
        }

        
    }
}
