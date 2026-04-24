using System.Text.Json;
using ClosedXML.Excel;
using QRCoder;
using 課堂打卡系統.Models;
using 課堂打卡系統.ViewModels;

namespace 課堂打卡系統.Services;

public interface IAttendanceQueryService
{
    Task<AttendanceDashboardViewModel> GetDashboardAsync(bool isManagementEnabled, CancellationToken cancellationToken = default);

    Task<CheckInPageViewModel?> GetCheckInPageAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<AttendanceResult> SubmitCheckInAsync(Guid sessionId, string studentNumber, string studentName, string? note, CancellationToken cancellationToken = default);

    Task<OperationResult> ToggleSessionStatusAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<AdminDashboardViewModel> GetAdminDashboardAsync(CancellationToken cancellationToken = default);

    Task<CourseFormViewModel> GetCourseFormAsync(Guid? courseId, CancellationToken cancellationToken = default);

    Task<OperationResult> SaveCourseAsync(CourseFormViewModel form, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteCourseAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<SessionFormViewModel> GetSessionFormAsync(Guid? sessionId, CancellationToken cancellationToken = default);

    Task<OperationResult> SaveSessionAsync(SessionFormViewModel form, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<AttendanceExportPayload?> ExportSessionAttendanceAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<QrBoardViewModel?> GetQrBoardAsync(Guid sessionId, string checkInUrl, CancellationToken cancellationToken = default);
}

public sealed class AttendanceQueryService : IAttendanceQueryService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly TimeProvider _timeProvider;

    public AttendanceQueryService(IHostEnvironment environment, TimeProvider timeProvider)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _storePath = Path.Combine(dataDirectory, "attendance-data.json");
        _timeProvider = timeProvider;
    }

    public async Task<AttendanceDashboardViewModel> GetDashboardAsync(bool isManagementEnabled, CancellationToken cancellationToken = default)
    {
        var store = await LoadStoreAsync(cancellationToken);
        return BuildDashboard(store, isManagementEnabled);
    }

    public async Task<CheckInPageViewModel?> GetCheckInPageAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var store = await LoadStoreAsync(cancellationToken);
        return BuildCheckInPage(store, sessionId);
    }

    public async Task<AttendanceResult> SubmitCheckInAsync(Guid sessionId, string studentNumber, string studentName, string? note, CancellationToken cancellationToken = default)
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

            var normalizedStudentNumber = studentNumber.Trim().ToUpperInvariant();
            var normalizedStudentName = studentName.Trim();

            if (string.IsNullOrWhiteSpace(normalizedStudentNumber) || string.IsNullOrWhiteSpace(normalizedStudentName))
            {
                return new AttendanceResult { Success = false, Message = "學號與姓名不可為空白。" };
            }

            var duplicated = store.Records.Any(record =>
                record.SessionId == sessionId &&
                string.Equals(record.StudentNumber, normalizedStudentNumber, StringComparison.OrdinalIgnoreCase));

            if (duplicated)
            {
                return new AttendanceResult { Success = false, Message = "此學號已完成本堂課打卡，請勿重複提交。" };
            }

            store.Records.Add(new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                StudentNumber = normalizedStudentNumber,
                StudentName = normalizedStudentName,
                CheckedInAt = _timeProvider.GetLocalNow(),
                Note = (note ?? string.Empty).Trim()
            });

            await SaveStoreUnsafeAsync(store, cancellationToken);
            return new AttendanceResult { Success = true, Message = "打卡成功，已完成出席登記。" };
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
            .OrderBy(course => course.CourseCode)
            .Select(course => new AdminCourseItemViewModel
            {
                Id = course.Id,
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
                var course = store.Courses.First(course => course.Id == session.CourseId);
                return new AdminSessionItemViewModel
                {
                    Id = session.Id,
                    CourseId = course.Id,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    TeacherName = course.TeacherName,
                    Classroom = course.Classroom,
                    Topic = session.Topic,
                    StartAt = session.StartAt.LocalDateTime,
                    EndAt = session.EndAt.LocalDateTime,
                    IsOpen = session.IsOpen,
                    AttendanceCount = store.Records.Count(record => record.SessionId == session.Id)
                };
            })
            .ToList();

        return new AdminDashboardViewModel
        {
            GeneratedAt = _timeProvider.GetLocalNow().LocalDateTime,
            CourseCount = courseItems.Count,
            SessionCount = sessionItems.Count,
            AttendanceCount = store.Records.Count,
            Courses = courseItems,
            Sessions = sessionItems
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
                EndAt = _timeProvider.GetLocalNow().LocalDateTime.AddHours(2)
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
                    IsOpen = form.IsOpen
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
        sheet.Cell(headerRow, 5).Value = "備註";

        for (var index = 0; index < records.Count; index++)
        {
            var row = headerRow + 1 + index;
            var record = records[index];
            sheet.Cell(row, 1).Value = index + 1;
            sheet.Cell(row, 2).Value = record.StudentNumber;
            sheet.Cell(row, 3).Value = record.StudentName;
            sheet.Cell(row, 4).Value = record.CheckedInAt.LocalDateTime.ToString("yyyy/MM/dd HH:mm:ss");
            sheet.Cell(row, 5).Value = record.Note;
        }

        sheet.Range(1, 1, headerRow, 5).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new AttendanceExportPayload
        {
            FileName = $"{SanitizeFileName(course.CourseCode)}_{SanitizeFileName(session.Topic)}_{session.StartAt:yyyyMMddHHmm}.xlsx",
            Content = stream.ToArray()
        };
    }

    public async Task<QrBoardViewModel?> GetQrBoardAsync(Guid sessionId, string checkInUrl, CancellationToken cancellationToken = default)
    {
        var store = await LoadStoreAsync(cancellationToken);
        var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null)
        {
            return null;
        }

        var course = store.Courses.First(courseItem => courseItem.Id == session.CourseId);
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
            EndAt = session.EndAt.LocalDateTime
        };
    }

    private AttendanceDashboardViewModel BuildDashboard(AttendanceStore store, bool isManagementEnabled)
    {
        var localNow = _timeProvider.GetLocalNow();
        var sessionCards = store.Sessions
            .OrderBy(session => session.StartAt)
            .Select(session =>
            {
                var course = store.Courses.First(course => course.Id == session.CourseId);
                var records = store.Records
                    .Where(record => record.SessionId == session.Id)
                    .OrderByDescending(record => record.CheckedInAt)
                    .ToList();

                return new SessionCardViewModel
                {
                    SessionId = session.Id,
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
                            Note = record.Note
                        })
                    ]
                };
            })
            .ToList();

        return new AttendanceDashboardViewModel
        {
            GeneratedAt = localNow.LocalDateTime,
            OpenSessionCount = sessionCards.Count(session => session.IsOpen),
            TotalAttendanceCount = store.Records.Count,
            Sessions = sessionCards
        };
    }

    private CheckInPageViewModel? BuildCheckInPage(AttendanceStore store, Guid sessionId)
    {
        var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
        if (session is null)
        {
            return null;
        }

        var course = store.Courses.First(course => course.Id == session.CourseId);
        var records = store.Records
            .Where(record => record.SessionId == sessionId)
            .OrderByDescending(record => record.CheckedInAt)
            .Select(record => new AttendanceRecordViewModel
            {
                StudentNumber = record.StudentNumber,
                StudentName = record.StudentName,
                CheckedInAt = record.CheckedInAt.LocalDateTime,
                Note = record.Note
            })
            .ToList();

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
            Records = records,
            Form = new CheckInFormViewModel
            {
                SessionId = session.Id
            }
        };
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
                    DisplayName = $"{course.CourseCode}｜{course.CourseName}"
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
        return store ?? BuildSeedStore();
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
            CourseCode = "CS101",
            CourseName = "資訊系統導論",
            TeacherName = "林冠廷",
            Classroom = "資電館 A201",
            Description = "系統思維、需求分析與資訊流程基礎。"
        };

        var course2 = new AttendanceCourse
        {
            Id = Guid.Parse("858bb6db-02b0-43c6-a4a7-47882dcf3c4f"),
            CourseCode = "SE305",
            CourseName = "軟體工程實務",
            TeacherName = "陳宜蓁",
            Classroom = "工程館 B105",
            Description = "專案管理、版本控制與團隊開發流程。"
        };

        var todayStart = new DateTimeOffset(today.AddHours(9), offset);
        var afternoonStart = new DateTimeOffset(today.AddHours(14), offset);
        var tomorrowStart = new DateTimeOffset(today.AddDays(1).AddHours(10), offset);

        return new AttendanceStore
        {
            Courses = [course1, course2],
            Sessions =
            [
                new ClassSession
                {
                    Id = Guid.Parse("75f20552-a27f-4498-b88b-21fdf19ff19f"),
                    CourseId = course1.Id,
                    StartAt = todayStart,
                    EndAt = todayStart.AddHours(2),
                    IsOpen = true,
                    Topic = "第一週：資訊系統角色與案例分析"
                },
                new ClassSession
                {
                    Id = Guid.Parse("c33d4f7c-5cbc-4414-9cca-b7f46f844a66"),
                    CourseId = course2.Id,
                    StartAt = afternoonStart,
                    EndAt = afternoonStart.AddHours(3),
                    IsOpen = false,
                    Topic = "Sprint 規劃與需求拆解演練"
                },
                new ClassSession
                {
                    Id = Guid.Parse("76925d36-43e4-4f07-b51d-3668615dba46"),
                    CourseId = course1.Id,
                    StartAt = tomorrowStart,
                    EndAt = tomorrowStart.AddHours(2),
                    IsOpen = false,
                    Topic = "第二週：校務流程數位化分組討論"
                }
            ]
        };
    }

    private static string SanitizeFileName(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(input.Select(character => invalid.Contains(character) ? '_' : character)).Trim();
    }
}
