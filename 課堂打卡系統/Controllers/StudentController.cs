using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using 課堂打卡系統.Services;
using 課堂打卡系統.ViewModels;

namespace 課堂打卡系統.Controllers;

public sealed class StudentController : Controller
{
    private readonly IStudentAuthService _studentAuthService;

    public StudentController(IStudentAuthService studentAuthService)
    {
        _studentAuthService = studentAuthService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.IsInRole(AuthClaimTypes.RoleStudent))
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new StudentLoginViewModel
        {
            ReturnUrl = returnUrl ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(StudentLoginViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var student = _studentAuthService.ValidateCredentials(form.StudentNumber, form.Password);
        if (student is null)
        {
            ModelState.AddModelError(string.Empty, "學號或密碼錯誤。");
            return View(form);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, student.StudentNumber),
            new(ClaimTypes.GivenName, student.StudentName),
            new(ClaimTypes.Role, AuthClaimTypes.RoleStudent),
            new(AuthClaimTypes.StudentNumber, student.StudentNumber),
            new(AuthClaimTypes.StudentName, student.StudentName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToLocal(form.ReturnUrl);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl!);
        }

        return RedirectToAction("Index", "Attendance");
    }
}
