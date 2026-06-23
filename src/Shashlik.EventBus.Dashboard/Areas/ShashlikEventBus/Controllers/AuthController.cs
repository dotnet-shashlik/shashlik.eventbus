using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
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

    public AuthController(IOptionsMonitor<EventBusDashboardOption> options)
    {
        _options = options;
    }

    [ViewData] public string UrlPrefix => _options.CurrentValue.UrlPrefix;

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Index(SecretLoginModel secretLoginModel)
    {
        if (_options.CurrentValue.AuthenticateProvider == typeof(SecretCookieAuthenticate)
            && !string.IsNullOrWhiteSpace(_options.CurrentValue.AuthenticateSecret)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(secretLoginModel.Secret ?? string.Empty),
                Encoding.ASCII.GetBytes(_options.CurrentValue.AuthenticateSecret)))
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            using var hmac = new HMACSHA256(Encoding.ASCII.GetBytes(_options.CurrentValue.AuthenticateSecret));
            var signatureBytes = hmac.ComputeHash(tokenBytes);

            var cookieValue = Convert.ToBase64String(tokenBytes) + "." + Convert.ToBase64String(signatureBytes);

            Response.Cookies.Append(
                _options.CurrentValue.AuthenticateSecretCookieName ?? EventBusDashboardOption.DefaultCookieName,
                cookieValue,
                _options.CurrentValue.AuthenticateSecretCookieOptions?.Invoke(HttpContext) ?? new CookieOptions
                    { Expires = DateTimeOffset.Now.AddHours(2) });

            return RedirectToAction("Index", "Published");
        }

        ViewBag.Error = "Secret error!";
        return View("Index");
    }
}