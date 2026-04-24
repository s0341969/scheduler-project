using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using 課堂打卡系統.Services;
using 課堂打卡系統.ViewModels;

namespace 課堂打卡系統.Controllers;

public sealed class AccountController : Controller
{
    private readonly IAdminAuthService _adminAuthService;

    public AccountController(IAdminAuthService adminAuthService)
    {
        _adminAuthService = adminAuthService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Admin");
        }

        return View(new LoginViewModel
        {
            Username = _adminAuthService.GetUsername(),
            ReturnUrl = returnUrl ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        if (!_adminAuthService.ValidateCredentials(form.Username, form.Password))
        {
            ModelState.AddModelError(string.Empty, "帳號或密碼錯誤。");
            return View(form);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, _adminAuthService.GetUsername()),
            new(ClaimTypes.GivenName, _adminAuthService.GetDisplayName()),
            new(ClaimTypes.Role, AuthClaimTypes.RoleAdministrator)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (Url.IsLocalUrl(form.ReturnUrl))
        {
            return Redirect(form.ReturnUrl);
        }

        return RedirectToAction("Index", "Admin");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Attendance");
    }
}
