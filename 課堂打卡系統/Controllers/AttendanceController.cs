using Microsoft.AspNetCore.Mvc;
using 課堂打卡系統.Services;
using 課堂打卡系統.ViewModels;

namespace 課堂打卡系統.Controllers;

public sealed class AttendanceController : Controller
{
    private readonly IAttendanceQueryService _attendanceQueryService;

    public AttendanceController(IAttendanceQueryService attendanceQueryService)
    {
        _attendanceQueryService = attendanceQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var viewModel = await _attendanceQueryService.GetDashboardAsync(cancellationToken);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken cancellationToken)
    {
        var viewModel = await _attendanceQueryService.GetCheckInPageAsync(id, cancellationToken);
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
        var page = await _attendanceQueryService.GetCheckInPageAsync(form.SessionId, cancellationToken);
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
                Records = page.Records,
                Form = form
            };

            return View(invalidViewModel);
        }

        var result = await _attendanceQueryService.SubmitCheckInAsync(form.SessionId, form.StudentNumber, form.StudentName, form.Note, cancellationToken);
        TempData[result.Success ? "StatusMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(CheckIn), new { id = form.SessionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSessionStatus(Guid sessionId, CancellationToken cancellationToken)
    {
        await _attendanceQueryService.ToggleSessionStatusAsync(sessionId, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
