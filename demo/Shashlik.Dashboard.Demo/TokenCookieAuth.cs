using System.Security.Principal;
using Microsoft.AspNetCore.Authentication;
using Shashlik.EventBus.Dashboard;

namespace Shashlik.Dashboard.Demo
{
    public class TokenCookieAuth : IEventBusDashboardAuthorize
    {
        private const string Token = "token123";

        public bool Authorize(HttpContext context)
        {
            var token = "";
            if (context.Request.Query.ContainsKey("token"))
            {
                token = context.Request.Query["token"];
            }
            else if(context.Request.Cookies.ContainsKey("token"))
            {
                token = context.Request.Cookies["token"];
            }

            if (token == Token)
            {
                if (!context.Request.Cookies.ContainsKey("token"))
                {
                    context.Response.Cookies.Append("token", token);
                }

                return true;
            }

            return false;
        }
    }
}
