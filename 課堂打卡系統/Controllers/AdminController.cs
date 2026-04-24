using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using 課堂打卡系統.Services;
using 課堂打卡系統.ViewModels;

namespace 課堂打卡系統.Controllers;

[Authorize(Roles = AuthClaimTypes.RoleAdministrator)]
public sealed class AdminController : Controller
{
    private readonly IAttendanceQueryService _attendanceQueryService;

    public AdminController(IAttendanceQueryService attendanceQueryService)
    {
        _attendanceQueryService = attendanceQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var viewModel = await _attendanceQueryService.GetAdminDashboardAsync(cancellationToken);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> CreateCourse(CancellationToken cancellationToken)
    {
        var viewModel = await _attendanceQueryService.GetCourseFormAsync(null, cancellationToken);
        return View("CourseForm", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> EditCourse(Guid id, CancellationToken cancellationToken)
    {
        var viewModel = await _attendanceQueryService.GetCourseFormAsync(id, cancellationToken);
        return View("CourseForm", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCourse(CourseFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("CourseForm", form);
        }

        var result = await _attendanceQueryService.SaveCourseAsync(form, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("CourseForm", form);
        }

        TempData["StatusMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourse(Guid id, CancellationToken cancellationToken)
    {
        var result = await _attendanceQueryService.DeleteCourseAsync(id, cancellationToken);
        TempData[result.Success ? "StatusMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> CreateSession(CancellationToken cancellationToken)
    {
        var viewModel = await _attendanceQueryService.GetSessionFormAsync(null, cancellationToken);
        return View("SessionForm", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> EditSession(Guid id, CancellationToken cancellationToken)
    {
        var viewModel = await _attendanceQueryService.GetSessionFormAsync(id, cancellationToken);
        return View("SessionForm", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSession(SessionFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var retryViewModel = await _attendanceQueryService.GetSessionFormAsync(form.Id, cancellationToken);
            var invalidViewModel = new SessionFormViewModel
            {
                Id = form.Id,
                CourseId = form.CourseId,
                Topic = form.Topic,
                StartAt = form.StartAt,
                EndAt = form.EndAt,
                IsOpen = form.IsOpen,
                CourseOptions = retryViewModel.CourseOptions
            };
            return View("SessionForm", invalidViewModel);
        }

        var result = await _attendanceQueryService.SaveSessionAsync(form, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            var retryViewModel = await _attendanceQueryService.GetSessionFormAsync(form.Id, cancellationToken);
            var invalidViewModel = new SessionFormViewModel
            {
                Id = form.Id,
                CourseId = form.CourseId,
                Topic = form.Topic,
                StartAt = form.StartAt,
                EndAt = form.EndAt,
                IsOpen = form.IsOpen,
                CourseOptions = retryViewModel.CourseOptions
            };
            return View("SessionForm", invalidViewModel);
        }

        TempData["StatusMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSession(Guid id, CancellationToken cancellationToken)
    {
        var result = await _attendanceQueryService.DeleteSessionAsync(id, cancellationToken);
        TempData[result.Success ? "StatusMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportSessionAttendance(Guid id, CancellationToken cancellationToken)
    {
        var payload = await _attendanceQueryService.ExportSessionAttendanceAsync(id, cancellationToken);
        if (payload is null)
        {
            return NotFound();
        }

        return File(payload.Content, payload.ContentType, payload.FileName);
    }
}
