using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Controllers;

public sealed class AuthController(
    ApplicationDbContext dbContext,
    IPasswordHasher<User> passwordHasher) : Controller
{
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

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "帳號不存在、未啟用或密碼不正確。");
            return View();
        }

        var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verifyResult == PasswordVerificationResult.Failed)
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

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View();

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            ModelState.AddModelError(string.Empty, "請完整輸入目前密碼、新密碼與確認密碼。");
            return View();
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "新密碼與確認密碼不一致。");
            return View();
        }

        if (newPassword.Length < 10)
        {
            ModelState.AddModelError(string.Empty, "新密碼長度至少需 10 碼。");
            return View();
        }

        var account = User.Identity?.Name ?? string.Empty;
        var user = await dbContext.Users.FirstOrDefaultAsync(item => item.Account == account && item.IsActive, cancellationToken);
        if (user is null)
        {
            return Challenge();
        }

        var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "目前密碼不正確。");
            return View();
        }

        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["StatusMessage"] = "密碼已更新。";
        return RedirectToAction("Index", "Dashboard");
    }

    public IActionResult AccessDenied() => View();
}
