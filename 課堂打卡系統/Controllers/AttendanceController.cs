using Microsoft.AspNetCore.Mvc;
using 課堂打卡系統.Services;
using 課堂打卡系統.ViewModels;

namespace 課堂打卡系統.Controllers;

public sealed class AttendanceController : Controller
{
    private const string DeviceCookieName = "ClassAttendance.DeviceId";

    private readonly IAttendanceQueryService _attendanceQueryService;

    public AttendanceController(IAttendanceQueryService attendanceQueryService)
    {
        _attendanceQueryService = attendanceQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var viewModel = await _attendanceQueryService.GetDashboardAsync(User.IsInRole(AuthClaimTypes.RoleAdministrator), cancellationToken);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> CheckIn(Guid id, string? token, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(AuthClaimTypes.RoleStudent))
        {
            return RedirectToAction("Login", "Student", new
            {
                returnUrl = Url.Action(nameof(CheckIn), "Attendance", new { id, token })
            });
        }

        EnsureDeviceCookie();
        var student = GetCurrentStudent();
        var viewModel = await _attendanceQueryService.GetCheckInPageAsync(id, token, student, GetSourceIp(), cancellationToken);
        if (viewModel is null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> QrBoard(Guid id, CancellationToken cancellationToken)
    {
        var checkInBaseUrl = Url.Action(nameof(CheckIn), "Attendance", values: null, Request.Scheme);
        if (string.IsNullOrWhiteSpace(checkInBaseUrl))
        {
            return NotFound();
        }

        var viewModel = await _attendanceQueryService.GetQrBoardAsync(id, checkInBaseUrl, cancellationToken);
        if (viewModel is null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(CheckInFormViewModel form, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(AuthClaimTypes.RoleStudent))
        {
            return RedirectToAction("Login", "Student", new
            {
                returnUrl = Url.Action(nameof(CheckIn), "Attendance", new { id = form.SessionId, token = form.Token })
            });
        }

        EnsureDeviceCookie();
        var student = GetCurrentStudent();
        if (student is null)
        {
            return RedirectToAction("Login", "Student");
        }

        var page = await _attendanceQueryService.GetCheckInPageAsync(form.SessionId, form.Token, student, GetSourceIp(), cancellationToken);
        if (page is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var invalidViewModel = new CheckInPageViewModel
            {
                SessionId = page.SessionId,
                CourseCode = page.CourseCode,
                CourseName = page.CourseName,
                TeacherName = page.TeacherName,
                Classroom = page.Classroom,
                Topic = page.Topic,
                StartAt = page.StartAt,
                EndAt = page.EndAt,
                IsOpen = page.IsOpen,
                RequireStudentLogin = page.RequireStudentLogin,
                CanCheckIn = page.CanCheckIn,
                IsQrValidated = page.IsQrValidated,
                IsWithinAllowedNetwork = page.IsWithinAllowedNetwork,
                ExpectedSsidLabel = page.ExpectedSsidLabel,
                AccessError = page.AccessError,
                StudentNumber = page.StudentNumber,
                StudentName = page.StudentName,
                QrExpiresAtLocal = page.QrExpiresAtLocal,
                Records = page.Records,
                Form = form
            };

            return View(invalidViewModel);
        }

        var result = await _attendanceQueryService.SubmitCheckInAsync(
            form.SessionId,
            form.Token,
            student,
            form.Note,
            GetSourceIp(),
            Request.Headers.UserAgent.ToString(),
            Request.Cookies[DeviceCookieName] ?? string.Empty,
            cancellationToken);

        TempData[result.Success ? "StatusMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(CheckIn), new { id = form.SessionId, token = form.Token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSessionStatus(Guid sessionId, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(AuthClaimTypes.RoleAdministrator))
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Admin") });
        }

        var result = await _attendanceQueryService.ToggleSessionStatusAsync(sessionId, cancellationToken);
        TempData[result.Success ? "StatusMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private StudentIdentity? GetCurrentStudent()
    {
        var studentNumber = User.FindFirst(AuthClaimTypes.StudentNumber)?.Value;
        var studentName = User.FindFirst(AuthClaimTypes.StudentName)?.Value;
        if (string.IsNullOrWhiteSpace(studentNumber) || string.IsNullOrWhiteSpace(studentName))
        {
            return null;
        }

        return new StudentIdentity
        {
            StudentNumber = studentNumber,
            StudentName = studentName
        };
    }

    private void EnsureDeviceCookie()
    {
        if (Request.Cookies.ContainsKey(DeviceCookieName))
        {
            return;
        }

        Response.Cookies.Append(DeviceCookieName, Guid.NewGuid().ToString("N"), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddMonths(6)
        });
    }

    private string GetSourceIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }
}
