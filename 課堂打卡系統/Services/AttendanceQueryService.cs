using System.Text.Json;
using 課堂打卡系統.Models;
using 課堂打卡系統.ViewModels;

namespace 課堂打卡系統.Services;

public interface IAttendanceQueryService
{
    Task<AttendanceDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<CheckInPageViewModel?> GetCheckInPageAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<AttendanceResult> SubmitCheckInAsync(Guid sessionId, string studentNumber, string studentName, string? note, CancellationToken cancellationToken = default);

    Task ToggleSessionStatusAsync(Guid sessionId, CancellationToken cancellationToken = default);
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

    public async Task<AttendanceDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var store = await LoadStoreAsync(cancellationToken);
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

    public async Task<CheckInPageViewModel?> GetCheckInPageAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var store = await LoadStoreAsync(cancellationToken);
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

    public async Task ToggleSessionStatusAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var store = await LoadStoreUnsafeAsync(cancellationToken);
            var session = store.Sessions.FirstOrDefault(item => item.Id == sessionId);
            if (session is null)
            {
                return;
            }

            session.IsOpen = !session.IsOpen;
            await SaveStoreUnsafeAsync(store, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
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
}
