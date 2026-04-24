using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Extensions.Options;
using QRCoder;
using 課堂打卡系統.Models;
using 課堂打卡系統.Options;
using 課堂打卡系統.ViewModels;

namespace 課堂打卡系統.Services;

public interface IAttendanceQueryService
{
    Task<AttendanceDashboardViewModel> GetDashboardAsync(bool isManagementEnabled, CancellationToken cancellationToken = default);

    Task<CheckInPageViewModel?> GetCheckInPageAsync(Guid sessionId, string? token, StudentIdentity? student, string sourceIp, CancellationToken cancellationToken = default);

    Task<AttendanceResult> SubmitCheckInAsync(Guid sessionId, string token, StudentIdentity student, string? note, string sourceIp, string userAgent, string deviceId, CancellationToken cancellationToken = default);

    Task<OperationResult> ToggleSessionStatusAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<AdminDashboardViewModel> GetAdminDashboardAsync(CancellationToken cancellationToken = default);

    Task<CourseFormViewModel> GetCourseFormAsync(Guid? courseId, CancellationToken cancellationToken = default);

    Task<OperationResult> SaveCourseAsync(CourseFormViewModel form, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteCourseAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<SessionFormViewModel> GetSessionFormAsync(Guid? sessionId, CancellationToken cancellationToken = default);

    Task<OperationResult> SaveSessionAsync(SessionFormViewModel form, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<AttendanceExportPayload?> ExportSessionAttendanceAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<QrBoardViewModel?> GetQrBoardAsync(Guid sessionId, string checkInBaseUrl, CancellationToken cancellationToken = default);
}

public sealed class AttendanceQueryService : IAttendanceQueryService
{
    private static readonly int[] SupportedGrades = [1, 2, 3];
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly TimeProvider _timeProvider;
    private readonly IQrTokenService _qrTokenService;
    private readonly AttendanceSecurityOptions _securityOptions;

    public AttendanceQueryService(
        IHostEnvironment environment,
        TimeProvider timeProvider,
        IQrTokenService qrTokenService,
        IOptions<AttendanceSecurityOptions> securityOptions)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _storePath = Path.Combine(dataDirectory, "attendance-data.json");
        _timeProvider = timeProvider;
        _qrTokenService = qrTokenService;
        _securityOptions = securityOptions.Value;
    }

    public async Task<AttendanceDashboardViewModel> GetDashboardAsync(bool isManagementEnabled, CancellationToken cancellationToken = default)
    {
        var store = await LoadStoreAsync(cancellationToken);
        return BuildDashboard(store, isManagementEnabled);
    }

    public async Task<CheckInPageViewModel?> GetCheckInPageAsync(Guid sessionId, string? token, StudentIdentity? student, string sourceIp, CancellationToken cancellationToken = default)
    {
        var store = await LoadStoreAsync(cancellationToken);
        var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null)
        {
            return null;
        }

        var course = store.Courses.First(courseItem => courseItem.Id == session.CourseId);
        var records = BuildRecordViewModels(store, sessionId);
        var qrValidation = _qrTokenService.ValidateToken(sessionId, token);
        var isWithinAllowedNetwork = IsAllowedIp(sourceIp);
        var canCheckIn = session.IsOpen && qrValidation.IsValid && student is not null && (!_securityOptions.StrictNetworkValidation || isWithinAllowedNetwork);
        var accessError = ResolveAccessError(session, qrValidation, student, isWithinAllowedNetwork);

        return new CheckInPageViewModel
        {
            SessionId = session.Id,
            CourseCode = course.CourseCode,
            CourseName = course.CourseName,
            TeacherName = course.TeacherName,
            Classroom = course.Classroom,
            Topic = session.Topic,
            StartAt = session.StartAt.LocalDateTime,
            EndAt = session.EndAt.LocalDateTime,
            IsOpen = session.IsOpen,
            RequireStudentLogin = session.RequireStudentLogin,
            CanCheckIn = canCheckIn,
            IsQrValidated = qrValidation.IsValid,
            IsWithinAllowedNetwork = isWithinAllowedNetwork,
            ExpectedSsidLabel = _securityOptions.ExpectedSsidLabel,
            AccessError = accessError,
            StudentNumber = student?.StudentNumber ?? string.Empty,
            StudentName = student?.StudentName ?? string.Empty,
            QrExpiresAtLocal = qrValidation.IsValid ? qrValidation.ExpiresAtUtc.LocalDateTime : null,
            Records = records,
            Form = new CheckInFormViewModel
            {
                SessionId = session.Id,
                Token = token ?? string.Empty
            }
        };
    }

    public async Task<AttendanceResult> SubmitCheckInAsync(Guid sessionId, string token, StudentIdentity student, string? note, string sourceIp, string userAgent, string deviceId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadStoreUnsafeAsync(cancellationToken);
            var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
            if (session is null)
            {
                return new AttendanceResult { Success = false, Message = "找不到指定的課堂時段。" };
            }

            if (!session.IsOpen)
            {
                return new AttendanceResult { Success = false, Message = "此課堂尚未開放或已關閉打卡。" };
            }

            var qrValidation = _qrTokenService.ValidateToken(sessionId, token);
            if (!qrValidation.IsValid)
            {
                return new AttendanceResult { Success = false, Message = qrValidation.ErrorMessage };
            }

            var normalizedStudentNumber = student.StudentNumber.Trim().ToUpperInvariant();
            var normalizedStudentName = student.StudentName.Trim();

            var duplicated = store.Records.Any(record =>
                record.SessionId == sessionId &&
                string.Equals(record.StudentNumber, normalizedStudentNumber, StringComparison.OrdinalIgnoreCase));

            if (duplicated)
            {
                return new AttendanceResult { Success = false, Message = "你已完成本堂課打卡，請勿重複提交。" };
            }

            var isWithinAllowedNetwork = IsAllowedIp(sourceIp);
            if (_securityOptions.StrictNetworkValidation && !isWithinAllowedNetwork)
            {
                return new AttendanceResult
                {
                    Success = false,
                    Message = $"目前來源網路不屬於 {_securityOptions.ExpectedSsidLabel} 的允許網段，請先連上班級 SSID 再打卡。"
                };
            }

            var fingerprint = ComputeDeviceFingerprint(deviceId, userAgent);
            var suspiciousReasons = new List<string>();
            if (!isWithinAllowedNetwork)
            {
                suspiciousReasons.Add($"來源 IP {sourceIp} 不在 {_securityOptions.ExpectedSsidLabel} 的允許網段內");
            }

            if (string.IsNullOrWhiteSpace(userAgent))
            {
                suspiciousReasons.Add("缺少 User-Agent");
            }

            var reusedDevice = store.Records.Any(record =>
                !string.IsNullOrWhiteSpace(record.DeviceFingerprint) &&
                string.Equals(record.DeviceFingerprint, fingerprint, StringComparison.Ordinal) &&
                !string.Equals(record.StudentNumber, normalizedStudentNumber, StringComparison.OrdinalIgnoreCase) &&
                record.CheckedInAt >= _timeProvider.GetLocalNow().AddHours(-12));

            if (reusedDevice)
            {
                suspiciousReasons.Add("同一裝置近期曾被不同學號使用");
            }

            store.Records.Add(new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                StudentNumber = normalizedStudentNumber,
                StudentName = normalizedStudentName,
                CheckedInAt = _timeProvider.GetLocalNow(),
                Note = (note ?? string.Empty).Trim(),
                SourceIp = sourceIp,
                UserAgent = userAgent,
                DeviceId = deviceId,
                DeviceFingerprint = fingerprint,
                QrIssuedAtUtc = qrValidation.IssuedAtUtc,
                QrExpiresAtUtc = qrValidation.ExpiresAtUtc,
                IsSuspicious = suspiciousReasons.Count > 0,
                SuspiciousReasons = suspiciousReasons,
                IsWithinAllowedNetwork = isWithinAllowedNetwork
            });

            await SaveStoreUnsafeAsync(store, cancellationToken);
            var resultMessage = suspiciousReasons.Count > 0
                ? "打卡成功，但此筆紀錄已被標示為可疑，管理者將於後台複核。"
                : "打卡成功，已完成本人出席登記。";
            return new AttendanceResult { Success = true, Message = resultMessage };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult> ToggleSessionStatusAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadStoreUnsafeAsync(cancellationToken);
            var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
            if (session is null)
            {
                return new OperationResult { Success = false, Message = "找不到指定課堂。" };
            }

            session.IsOpen = !session.IsOpen;
            await SaveStoreUnsafeAsync(store, cancellationToken);
            return new OperationResult
            {
                Success = true,
                Message = session.IsOpen ? "已開放該堂課打卡。" : "已關閉該堂課打卡。"
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        var store = await LoadStoreAsync(cancellationToken);
        var courseItems = store.Courses
            .OrderBy(course => course.ClassCode)
            .ThenBy(course => course.CourseCode)
            .Select(course => new AdminCourseItemViewModel
            {
                Id = course.Id,
                ClassCode = course.ClassCode,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                TeacherName = course.TeacherName,
                Classroom = course.Classroom,
                Description = course.Description,
                SessionCount = store.Sessions.Count(session => session.CourseId == course.Id),
                AttendanceCount = store.Records.Count(record =>
                    store.Sessions.Any(session => session.Id == record.SessionId && session.CourseId == course.Id))
            })
            .ToList();

        var sessionItems = store.Sessions
            .OrderByDescending(session => session.StartAt)
            .Select(session =>
            {
                var course = store.Courses.First(courseItem => courseItem.Id == session.CourseId);
                return new AdminSessionItemViewModel
                {
                    Id = session.Id,
                    CourseId = course.Id,
                    ClassCode = course.ClassCode,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    TeacherName = course.TeacherName,
                    Classroom = course.Classroom,
                    Topic = session.Topic,
                    StartAt = session.StartAt.LocalDateTime,
                    EndAt = session.EndAt.LocalDateTime,
                    IsOpen = session.IsOpen,
                    AttendanceCount = store.Records.Count(record => record.SessionId == session.Id),
                    SuspiciousAttendanceCount = store.Records.Count(record => record.SessionId == session.Id && record.IsSuspicious)
                };
            })
            .ToList();

        var suspiciousRecords = store.Records
            .Where(record => record.IsSuspicious)
            .OrderByDescending(record => record.CheckedInAt)
            .Take(12)
            .Select(record =>
            {
                var session = store.Sessions.First(item => item.Id == record.SessionId);
                var course = store.Courses.First(item => item.Id == session.CourseId);
                return new AdminSuspiciousRecordViewModel
                {
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    Topic = session.Topic,
                    StudentNumber = record.StudentNumber,
                    StudentName = record.StudentName,
                    CheckedInAt = record.CheckedInAt.LocalDateTime,
                    SourceIp = record.SourceIp,
                    SuspiciousReasonSummary = string.Join("、", record.SuspiciousReasons)
                };
            })
            .ToList();

        return new AdminDashboardViewModel
        {
            GeneratedAt = _timeProvider.GetLocalNow().LocalDateTime,
            CourseCount = courseItems.Count,
            SessionCount = sessionItems.Count,
            AttendanceCount = store.Records.Count,
            SuspiciousAttendanceCount = store.Records.Count(record => record.IsSuspicious),
            Courses = courseItems,
            Sessions = sessionItems,
            SuspiciousRecords = suspiciousRecords
        };
    }

    public async Task<CourseFormViewModel> GetCourseFormAsync(Guid? courseId, CancellationToken cancellationToken = default)
    {
        var store = await LoadStoreAsync(cancellationToken);
        if (!courseId.HasValue)
        {
            return new CourseFormViewModel();
        }

        var course = store.Courses.FirstOrDefault(item => item.Id == courseId.Value);
        if (course is null)
        {
            return new CourseFormViewModel();
        }

        return new CourseFormViewModel
        {
            Id = course.Id,
            ClassCode = course.ClassCode,
            CourseCode = course.CourseCode,
            CourseName = course.CourseName,
            TeacherName = course.TeacherName,
            Classroom = course.Classroom,
            Description = course.Description
        };
    }

    public async Task<OperationResult> SaveCourseAsync(CourseFormViewModel form, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadStoreUnsafeAsync(cancellationToken);
            var normalizedClassCode = form.ClassCode.Trim();
            var normalizedCode = form.CourseCode.Trim().ToUpperInvariant();

            var duplicated = store.Courses.Any(course =>
                course.Id != form.Id &&
                string.Equals(course.CourseCode, normalizedCode, StringComparison.OrdinalIgnoreCase));

            if (duplicated)
            {
                return new OperationResult { Success = false, Message = "課程代碼不可重複。" };
            }

            if (form.Id.HasValue)
            {
                var course = store.Courses.FirstOrDefault(item => item.Id == form.Id.Value);
                if (course is null)
                {
                    return new OperationResult { Success = false, Message = "找不到指定課程。" };
                }

                course.ClassCode = normalizedClassCode;
                course.CourseCode = normalizedCode;
                course.CourseName = form.CourseName.Trim();
                course.TeacherName = form.TeacherName.Trim();
                course.Classroom = form.Classroom.Trim();
                course.Description = form.Description.Trim();
            }
            else
            {
                store.Courses.Add(new AttendanceCourse
                {
                    Id = Guid.NewGuid(),
                    ClassCode = normalizedClassCode,
                    CourseCode = normalizedCode,
                    CourseName = form.CourseName.Trim(),
                    TeacherName = form.TeacherName.Trim(),
                    Classroom = form.Classroom.Trim(),
                    Description = form.Description.Trim()
                });
            }

            await SaveStoreUnsafeAsync(store, cancellationToken);
            return new OperationResult { Success = true, Message = form.Id.HasValue ? "課程資料已更新。" : "課程已建立。" };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult> DeleteCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadStoreUnsafeAsync(cancellationToken);
            var course = store.Courses.FirstOrDefault(item => item.Id == courseId);
            if (course is null)
            {
                return new OperationResult { Success = false, Message = "找不到指定課程。" };
            }

            if (store.Sessions.Any(session => session.CourseId == courseId))
            {
                return new OperationResult { Success = false, Message = "此課程仍有課堂時段，請先刪除課堂後再刪除課程。" };
            }

            store.Courses.Remove(course);
            await SaveStoreUnsafeAsync(store, cancellationToken);
            return new OperationResult { Success = true, Message = "課程已刪除。" };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SessionFormViewModel> GetSessionFormAsync(Guid? sessionId, CancellationToken cancellationToken = default)
    {
        var store = await LoadStoreAsync(cancellationToken);
        var courseOptions = BuildCourseOptions(store);

        if (!sessionId.HasValue)
        {
            return new SessionFormViewModel
            {
                CourseOptions = courseOptions,
                StartAt = _timeProvider.GetLocalNow().LocalDateTime.AddMinutes(30),
                EndAt = _timeProvider.GetLocalNow().LocalDateTime.AddHours(2),
                IsOpen = false
            };
        }

        var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId.Value);
        if (session is null)
        {
            return new SessionFormViewModel { CourseOptions = courseOptions };
        }

        return new SessionFormViewModel
        {
            Id = session.Id,
            CourseId = session.CourseId,
            Topic = session.Topic,
            StartAt = session.StartAt.LocalDateTime,
            EndAt = session.EndAt.LocalDateTime,
            IsOpen = session.IsOpen,
            CourseOptions = courseOptions
        };
    }

    public async Task<OperationResult> SaveSessionAsync(SessionFormViewModel form, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadStoreUnsafeAsync(cancellationToken);
            if (!form.CourseId.HasValue || !form.StartAt.HasValue || !form.EndAt.HasValue)
            {
                return new OperationResult { Success = false, Message = "課堂資料不完整。" };
            }

            var course = store.Courses.FirstOrDefault(item => item.Id == form.CourseId.Value);
            if (course is null)
            {
                return new OperationResult { Success = false, Message = "找不到對應課程。" };
            }

            var offset = _timeProvider.GetLocalNow().Offset;
            var startAt = new DateTimeOffset(DateTime.SpecifyKind(form.StartAt.Value, DateTimeKind.Unspecified), offset);
            var endAt = new DateTimeOffset(DateTime.SpecifyKind(form.EndAt.Value, DateTimeKind.Unspecified), offset);

            if (form.Id.HasValue)
            {
                var session = store.Sessions.FirstOrDefault(item => item.Id == form.Id.Value);
                if (session is null)
                {
                    return new OperationResult { Success = false, Message = "找不到指定課堂。" };
                }

                session.CourseId = course.Id;
                session.Topic = form.Topic.Trim();
                session.StartAt = startAt;
                session.EndAt = endAt;
                session.IsOpen = form.IsOpen;
                session.RequireStudentLogin = true;
            }
            else
            {
                store.Sessions.Add(new ClassSession
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    Topic = form.Topic.Trim(),
                    StartAt = startAt,
                    EndAt = endAt,
                    IsOpen = form.IsOpen,
                    RequireStudentLogin = true
                });
            }

            await SaveStoreUnsafeAsync(store, cancellationToken);
            return new OperationResult { Success = true, Message = form.Id.HasValue ? "課堂資料已更新。" : "課堂已建立。" };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadStoreUnsafeAsync(cancellationToken);
            var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
            if (session is null)
            {
                return new OperationResult { Success = false, Message = "找不到指定課堂。" };
            }

            if (store.Records.Any(record => record.SessionId == sessionId))
            {
                return new OperationResult { Success = false, Message = "此課堂已有打卡紀錄，為避免資料遺失，暫不允許刪除。" };
            }

            store.Sessions.Remove(session);
            await SaveStoreUnsafeAsync(store, cancellationToken);
            return new OperationResult { Success = true, Message = "課堂已刪除。" };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AttendanceExportPayload?> ExportSessionAttendanceAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var store = await LoadStoreAsync(cancellationToken);
        var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null)
        {
            return null;
        }

        var course = store.Courses.First(courseItem => courseItem.Id == session.CourseId);
        var records = store.Records
            .Where(record => record.SessionId == sessionId)
            .OrderBy(record => record.CheckedInAt)
            .ToList();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("出席紀錄");
        sheet.Cell(1, 1).Value = "課程代碼";
        sheet.Cell(1, 2).Value = course.CourseCode;
        sheet.Cell(2, 1).Value = "課程名稱";
        sheet.Cell(2, 2).Value = course.CourseName;
        sheet.Cell(3, 1).Value = "課堂主題";
        sheet.Cell(3, 2).Value = session.Topic;
        sheet.Cell(4, 1).Value = "課堂時間";
        sheet.Cell(4, 2).Value = $"{session.StartAt.LocalDateTime:yyyy/MM/dd HH:mm} - {session.EndAt.LocalDateTime:HH:mm}";

        var headerRow = 6;
        sheet.Cell(headerRow, 1).Value = "序號";
        sheet.Cell(headerRow, 2).Value = "學號";
        sheet.Cell(headerRow, 3).Value = "姓名";
        sheet.Cell(headerRow, 4).Value = "打卡時間";
        sheet.Cell(headerRow, 5).Value = "IP";
        sheet.Cell(headerRow, 6).Value = "裝置指紋";
        sheet.Cell(headerRow, 7).Value = "網段狀態";
        sheet.Cell(headerRow, 8).Value = "可疑註記";
        sheet.Cell(headerRow, 9).Value = "備註";

        for (var index = 0; index < records.Count; index++)
        {
            var row = headerRow + 1 + index;
            var record = records[index];
            sheet.Cell(row, 1).Value = index + 1;
            sheet.Cell(row, 2).Value = record.StudentNumber;
            sheet.Cell(row, 3).Value = record.StudentName;
            sheet.Cell(row, 4).Value = record.CheckedInAt.LocalDateTime.ToString("yyyy/MM/dd HH:mm:ss");
            sheet.Cell(row, 5).Value = record.SourceIp;
            sheet.Cell(row, 6).Value = record.DeviceFingerprint;
            sheet.Cell(row, 7).Value = record.IsWithinAllowedNetwork ? "符合班級 SSID 網段" : "不在允許網段";
            sheet.Cell(row, 8).Value = record.IsSuspicious ? string.Join("、", record.SuspiciousReasons) : "無";
            sheet.Cell(row, 9).Value = record.Note;
        }

        sheet.Range(1, 1, headerRow, 9).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new AttendanceExportPayload
        {
            FileName = $"{SanitizeFileName(course.CourseCode)}_{SanitizeFileName(session.Topic)}_{session.StartAt:yyyyMMddHHmm}.xlsx",
            Content = stream.ToArray()
        };
    }

    public async Task<QrBoardViewModel?> GetQrBoardAsync(Guid sessionId, string checkInBaseUrl, CancellationToken cancellationToken = default)
    {
        var store = await LoadStoreAsync(cancellationToken);
        var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null)
        {
            return null;
        }

        var course = store.Courses.First(courseItem => courseItem.Id == session.CourseId);
        var token = _qrTokenService.CreateToken(sessionId);
        var checkInUrl = $"{checkInBaseUrl}?id={sessionId:D}&token={Uri.EscapeDataString(token.Token)}";
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(checkInUrl, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new SvgQRCode(qrData);

        return new QrBoardViewModel
        {
            SessionId = session.Id,
            CourseCode = course.CourseCode,
            CourseName = course.CourseName,
            Topic = session.Topic,
            Classroom = course.Classroom,
            CheckInUrl = checkInUrl,
            QrCodeSvg = qrCode.GetGraphic(12),
            StartAt = session.StartAt.LocalDateTime,
            EndAt = session.EndAt.LocalDateTime,
            ExpiresAt = token.ExpiresAtUtc.LocalDateTime,
            ExpectedSsidLabel = _securityOptions.ExpectedSsidLabel
        };
    }

    private AttendanceDashboardViewModel BuildDashboard(AttendanceStore store, bool isManagementEnabled)
    {
        var localNow = _timeProvider.GetLocalNow();
        var sessionCards = store.Sessions
            .OrderBy(session => session.StartAt)
            .Select(session =>
            {
                var course = store.Courses.First(courseItem => courseItem.Id == session.CourseId);
                var records = store.Records
                    .Where(record => record.SessionId == session.Id)
                    .OrderByDescending(record => record.CheckedInAt)
                    .ToList();

                return new SessionCardViewModel
                {
                    SessionId = session.Id,
                    ClassCode = course.ClassCode,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    TeacherName = course.TeacherName,
                    Classroom = course.Classroom,
                    Description = course.Description,
                    Topic = session.Topic,
                    StartAt = session.StartAt.LocalDateTime,
                    EndAt = session.EndAt.LocalDateTime,
                    IsOpen = session.IsOpen,
                    AttendanceCount = records.Count,
                    IsManagementEnabled = isManagementEnabled,
                    RecentStudents =
                    [
                        .. records.Take(5).Select(record => new AttendanceRecordViewModel
                        {
                            StudentNumber = record.StudentNumber,
                            StudentName = record.StudentName,
                            CheckedInAt = record.CheckedInAt.LocalDateTime,
                            Note = record.Note,
                            IsSuspicious = record.IsSuspicious,
                            SuspiciousReasonSummary = record.IsSuspicious ? string.Join("、", record.SuspiciousReasons) : string.Empty
                        })
                    ]
                };
            })
            .ToList();

        var gradeSections = SupportedGrades
            .Select(gradeYear => new GradeSectionViewModel
            {
                GradeYear = gradeYear,
                GradeLabel = GetGradeLabel(gradeYear),
                ClassSections =
                [
                    .. GetClassCodesForGrade(gradeYear).Select(classCode => new ClassSectionViewModel
                    {
                        ClassCode = classCode,
                        ClassLabel = $"{classCode} 班",
                        Sessions =
                        [
                            .. sessionCards
                                .Where(session => string.Equals(session.ClassCode, classCode, StringComparison.OrdinalIgnoreCase))
                                .OrderBy(session => session.StartAt)
                                .ThenBy(session => session.CourseCode)
                        ]
                    })
                ]
            })
            .ToList();

        return new AttendanceDashboardViewModel
        {
            GeneratedAt = localNow.LocalDateTime,
            OpenSessionCount = sessionCards.Count(session => session.IsOpen),
            TotalAttendanceCount = store.Records.Count,
            GradeSections = gradeSections
        };
    }

    private IReadOnlyList<AttendanceRecordViewModel> BuildRecordViewModels(AttendanceStore store, Guid sessionId)
    {
        return
        [
            .. store.Records
                .Where(record => record.SessionId == sessionId)
                .OrderByDescending(record => record.CheckedInAt)
                .Select(record => new AttendanceRecordViewModel
                {
                    StudentNumber = record.StudentNumber,
                    StudentName = record.StudentName,
                    CheckedInAt = record.CheckedInAt.LocalDateTime,
                    Note = record.Note,
                    IsSuspicious = record.IsSuspicious,
                    SuspiciousReasonSummary = record.IsSuspicious ? string.Join("、", record.SuspiciousReasons) : string.Empty
                })
        ];
    }

    private string ResolveAccessError(ClassSession session, QrTokenValidationResult qrValidation, StudentIdentity? student, bool isWithinAllowedNetwork)
    {
        if (!session.IsOpen)
        {
            return "此課堂目前未開放打卡。";
        }

        if (student is null)
        {
            return "請先使用學生帳號登入，系統才會將此次打卡視為本人出席。";
        }

        if (!qrValidation.IsValid)
        {
            return qrValidation.ErrorMessage;
        }

        if (_securityOptions.StrictNetworkValidation && !isWithinAllowedNetwork)
        {
            return $"你目前沒有連上 {_securityOptions.ExpectedSsidLabel} 的允許網段，請連上班級 SSID 後重新掃碼。";
        }

        if (!isWithinAllowedNetwork)
        {
            return $"目前來源網路不在 {_securityOptions.ExpectedSsidLabel} 的允許網段內，此次打卡若送出將被標示為可疑紀錄。";
        }

        return string.Empty;
    }

    private bool IsAllowedIp(string sourceIp)
    {
        if (string.IsNullOrWhiteSpace(sourceIp))
        {
            return false;
        }

        if (_securityOptions.AllowedIpPrefixes.Count == 0)
        {
            return true;
        }

        return _securityOptions.AllowedIpPrefixes.Any(prefix =>
            sourceIp.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static string ComputeDeviceFingerprint(string deviceId, string userAgent)
    {
        var raw = $"{deviceId}|{userAgent}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private IReadOnlyList<CourseOptionViewModel> BuildCourseOptions(AttendanceStore store)
    {
        return
        [
            .. store.Courses
                .OrderBy(course => course.CourseCode)
                .Select(course => new CourseOptionViewModel
                {
                    Id = course.Id,
                    DisplayName = $"{course.ClassCode}班｜{course.CourseCode}｜{course.CourseName}"
                })
        ];
    }

    private async Task<AttendanceStore> LoadStoreAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadStoreUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<AttendanceStore> LoadStoreUnsafeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            var seed = BuildSeedStore();
            await SaveStoreUnsafeAsync(seed, cancellationToken);
            return seed;
        }

        await using var stream = File.OpenRead(_storePath);
        var store = await JsonSerializer.DeserializeAsync<AttendanceStore>(stream, _jsonOptions, cancellationToken);
        var resolvedStore = store ?? BuildSeedStore();
        if (NormalizeStore(resolvedStore))
        {
            await SaveStoreUnsafeAsync(resolvedStore, cancellationToken);
        }

        return resolvedStore;
    }

    private async Task SaveStoreUnsafeAsync(AttendanceStore store, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_storePath);
        await JsonSerializer.SerializeAsync(stream, store, _jsonOptions, cancellationToken);
    }

    private AttendanceStore BuildSeedStore()
    {
        var current = _timeProvider.GetLocalNow();
        var today = current.Date;
        var offset = current.Offset;

        var course1 = new AttendanceCourse
        {
            Id = Guid.Parse("5d5114f2-d4f3-485e-8dbf-8bf97d09c930"),
            ClassCode = "101",
            CourseCode = "CS101",
            CourseName = "資訊系統導論",
            TeacherName = "林冠廷",
            Classroom = "資電館 A201",
            Description = "系統思維、需求分析與資訊流程基礎。"
        };

        var course2 = new AttendanceCourse
        {
            Id = Guid.Parse("858bb6db-02b0-43c6-a4a7-47882dcf3c4f"),
            ClassCode = "201",
            CourseCode = "SE305",
            CourseName = "軟體工程實務",
            TeacherName = "陳宜蓁",
            Classroom = "工程館 B105",
            Description = "專案管理、版本控制與團隊開發流程。"
        };

        var course3 = new AttendanceCourse
        {
            Id = Guid.Parse("ef948f5c-f680-42a8-9f48-b3d6e13ef9c2"),
            ClassCode = "301",
            CourseCode = "DS401",
            CourseName = "資料分析應用",
            TeacherName = "吳若寧",
            Classroom = "資訊館 C303",
            Description = "資料整理、視覺化與課堂案例演練。"
        };

        var todayStart = new DateTimeOffset(today.AddHours(9), offset);
        var afternoonStart = new DateTimeOffset(today.AddHours(14), offset);
        var tomorrowStart = new DateTimeOffset(today.AddDays(1).AddHours(10), offset);
        var tomorrowAfternoonStart = new DateTimeOffset(today.AddDays(1).AddHours(15), offset);

        return new AttendanceStore
        {
            Courses = [course1, course2, course3],
            Sessions =
            [
                new ClassSession
                {
                    Id = Guid.Parse("75f20552-a27f-4498-b88b-21fdf19ff19f"),
                    CourseId = course1.Id,
                    StartAt = todayStart,
                    EndAt = todayStart.AddHours(2),
                    IsOpen = true,
                    Topic = "第一週：資訊系統角色與案例分析",
                    RequireStudentLogin = true
                },
                new ClassSession
                {
                    Id = Guid.Parse("c33d4f7c-5cbc-4414-9cca-b7f46f844a66"),
                    CourseId = course2.Id,
                    StartAt = afternoonStart,
                    EndAt = afternoonStart.AddHours(3),
                    IsOpen = false,
                    Topic = "Sprint 規劃與需求拆解演練",
                    RequireStudentLogin = true
                },
                new ClassSession
                {
                    Id = Guid.Parse("76925d36-43e4-4f07-b51d-3668615dba46"),
                    CourseId = course1.Id,
                    StartAt = tomorrowStart,
                    EndAt = tomorrowStart.AddHours(2),
                    IsOpen = false,
                    Topic = "第二週：校務流程數位化分組討論",
                    RequireStudentLogin = true
                },
                new ClassSession
                {
                    Id = Guid.Parse("f28b2eb1-e4a2-4bc3-bf81-8ee4efa85a83"),
                    CourseId = course3.Id,
                    StartAt = tomorrowAfternoonStart,
                    EndAt = tomorrowAfternoonStart.AddHours(2),
                    IsOpen = false,
                    Topic = "儀表板指標解讀與班級案例分析",
                    RequireStudentLogin = true
                }
            ]
        };
    }

    private bool NormalizeStore(AttendanceStore store)
    {
        var changed = false;
        var usedClassCodes = new HashSet<string>(
            store.Courses
                .Select(course => course.ClassCode.Trim())
                .Where(IsSupportedClassCode),
            StringComparer.OrdinalIgnoreCase);
        var fallbackClassCodes = SupportedGrades
            .SelectMany(GetClassCodesForGrade)
            .ToList();
        var fallbackIndex = 0;

        foreach (var course in store.Courses.OrderBy(item => item.CourseCode, StringComparer.OrdinalIgnoreCase))
        {
            if (IsSupportedClassCode(course.ClassCode))
            {
                course.ClassCode = course.ClassCode.Trim();
                continue;
            }

            while (fallbackIndex < fallbackClassCodes.Count && usedClassCodes.Contains(fallbackClassCodes[fallbackIndex]))
            {
                fallbackIndex++;
            }

            var fallback = fallbackIndex < fallbackClassCodes.Count
                ? fallbackClassCodes[fallbackIndex]
                : "101";

            course.ClassCode = fallback;
            usedClassCodes.Add(fallback);
            changed = true;
        }

        return changed;
    }

    private static IReadOnlyList<string> GetClassCodesForGrade(int gradeYear)
    {
        return [.. Enumerable.Range(1, 10).Select(classNumber => $"{gradeYear}{classNumber:00}")];
    }

    private static string GetGradeLabel(int gradeYear)
    {
        return gradeYear switch
        {
            1 => "一年級",
            2 => "二年級",
            3 => "三年級",
            _ => $"{gradeYear} 年級"
        };
    }

    private static bool IsSupportedClassCode(string? classCode)
    {
        if (string.IsNullOrWhiteSpace(classCode) || classCode.Length != 3)
        {
            return false;
        }

        return classCode is "101" or "102" or "103" or "104" or "105" or "106" or "107" or "108" or "109" or "110"
            or "201" or "202" or "203" or "204" or "205" or "206" or "207" or "208" or "209" or "210"
            or "301" or "302" or "303" or "304" or "305" or "306" or "307" or "308" or "309" or "310";
    }

    private static string SanitizeFileName(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(input.Select(character => invalid.Contains(character) ? '_' : character)).Trim();
    }
}
