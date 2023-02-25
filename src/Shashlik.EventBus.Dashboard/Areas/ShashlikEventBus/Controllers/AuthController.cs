using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Models;

namespace Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Controllers;

/// <summary>
/// secret认证类
/// </summary>
[AllowAnonymous]
[Area(Consts.AreaName)]
public class AuthController : Controller
{
    private readonly IOptionsMonitor<EventBusDashboardOption> _options;
    private readonly IDataProtectionProvider _dataProtector;

    public AuthController(IOptionsMonitor<EventBusDashboardOption> options,
        IDataProtectionProvider dataProtector)
    {
        _options = options;
        _dataProtector = dataProtector;
    }

    [ViewData] public string UrlPrefix => _options.CurrentValue.UrlPrefix;

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Index(SecretLoginModel secretLoginModel)
    {
        if (secretLoginModel.Secret == _options.CurrentValue.AuthenticateSecret)
        {
            Response.Cookies.Append(
                _options.CurrentValue.AuthenticateSecretCookieName ?? EventBusDashboardOption.DefaultCookieName,
                _dataProtector.CreateProtector(EventBusDashboardOption.DataProtectorName)
                    .Protect(secretLoginModel.Secret),
                _options.CurrentValue.AuthenticateSecretCookieOptions?.Invoke() ?? new CookieOptions
                    { Expires = DateTimeOffset.Now.AddHours(2) });

            return RedirectToAction("Index", "Published");
        }

        ViewBag.Error = "Secret error!";
        return View("Index");
    }
}