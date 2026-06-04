using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Controllers;

public sealed class AuthController(
    ApplicationDbContext dbContext,
    IOptions<LocalAuthOptions> localAuthOptions) : Controller
{
    private readonly LocalAuthOptions _localAuthOptions = localAuthOptions.Value;

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string account, string password, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError(string.Empty, "請輸入帳號與密碼。");
            return View();
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Account == account && item.IsActive, cancellationToken);

        if (user is null || !string.Equals(password, _localAuthOptions.SharedPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "帳號不存在、未啟用或密碼不正確。");
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Account),
            new(ClaimTypes.GivenName, user.UserName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, user.RoleName),
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();
}
