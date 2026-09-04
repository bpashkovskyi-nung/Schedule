using Dekanat.ScheduleSdk;
using Dekanat.ScheduleSdk.Enums;
using Dekanat.ScheduleSdk.Exceptions;
using Dekanat.ScheduleSdk.Models;
using Dekanat.ScheduleSdk.Requests;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Schedule.Api.Configuration;
using Schedule.Contracts.Common;
using Schedule.Contracts.ReferenceData;
using Schedule.Contracts.Schedules;

namespace Schedule.Api.Services;

public sealed class ScheduleApiService(
    IScheduleDataSource dataSource,
    IOptions<ScheduleApiOptions> options,
    IMemoryCache cache) : IScheduleApiService
{
    private const string ReferenceCacheKey = "schedule-api:reference-data";
    private static readonly SemaphoreSlim ReferenceLock = new(1, 1);
    private readonly ScheduleApiOptions options = options.Value;

    public bool IsDatabaseSource => dataSource.IsDatabase;

    public async Task<ReferenceDataResponse> GetReferenceDataAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(ReferenceCacheKey, out ReferenceDataResponse? cached) && cached is not null)
        {
            return cached;
        }

        await ReferenceLock.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue(ReferenceCacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            Task<PsRozkladExport> roomsTask = dataSource.GetObjectListAsync(
                RequestMode.Room,
                cancellationToken);

            Task<PsRozkladExport> teachersTask = dataSource.GetObjectListAsync(
                RequestMode.Teacher,
                cancellationToken);

            Task<PsRozkladExport> groupsTask = dataSource.GetObjectListAsync(
                RequestMode.Group,
                cancellationToken);

            await Task.WhenAll(roomsTask, teachersTask, groupsTask);

            PsRozkladExport rooms = EnsureSuccessful(await roomsTask);
            PsRozkladExport teachers = EnsureSuccessful(await teachersTask);
            PsRozkladExport groups = EnsureSuccessful(await groupsTask);

            ReferenceDataResponse referenceData = new()
            {
                Buildings = MapBuildings(rooms),
                TeacherDepartments = MapDepartments(teachers),
                GroupDepartments = MapDepartments(groups),
            };

            cache.Set(
                ReferenceCacheKey,
                referenceData,
                TimeSpan.FromMinutes(Math.Max(1, options.ReferenceCacheMinutes)));

            return referenceData;
        }
        finally
        {
            ReferenceLock.Release();
        }
    }

    public async Task<PsRozkladExport> GetScheduleExportAsync(
        ScheduleTargetType targetType,
        string objectId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        PsRozkladExport export = await dataSource.GetScheduleAsync(
            ToRequestMode(targetType),
            objectId,
            from,
            to,
            cancellationToken);

        if (!export.IsSuccess)
        {
            throw new PsRozkladApiException(export);
        }

        return export;
    }

    public async Task<ScheduleResponse> GetScheduleAsync(
        ScheduleTargetType targetType,
        string objectId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        PsRozkladExport export = await GetScheduleExportAsync(targetType, objectId, from, to, cancellationToken);

        return new ScheduleResponse
        {
            TargetType = targetType,
            ObjectId = objectId,
            ObjectName = objectId,
            Period = new DateRangeDto(from, to),
            Lessons = (export.ScheduleItems ?? []).Select(MapLesson).ToList(),
        };
    }

    public async Task<byte[]> GenerateIcalAsync(
        ScheduleTargetType targetType,
        string objectId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        PsRozkladExport export = await GetScheduleExportAsync(
            targetType,
            objectId,
            from,
            to,
            cancellationToken);

        return ApiIcalGenerator.Generate(export, objectId);
    }

    public string BuildIcalUrl(ScheduleTargetType targetType, string objectId, DateOnly from, DateOnly to)
    {
        string mode = targetType switch
        {
            ScheduleTargetType.Teacher => "teacher",
            ScheduleTargetType.Group => "group",
            ScheduleTargetType.Room => "room",
            _ => throw new ArgumentOutOfRangeException(nameof(targetType)),
        };

        string begin = from.ToString("dd.MM.yyyy");
        string end = to.ToString("dd.MM.yyyy");

        return $"{options.TimetableBaseUrl}?req_type=rozklad&req_mode={mode}" +
               $"&OBJ_ID={Uri.EscapeDataString(objectId)}&OBJ_name=&dep_name=&ros_text=united" +
               $"&begin_date={begin}&end_date={end}&req_format=iCal&coding_mode=UTF8&bs=ok";
    }

    private static RequestMode ToRequestMode(ScheduleTargetType targetType) => targetType switch
    {
        ScheduleTargetType.Teacher => RequestMode.Teacher,
        ScheduleTargetType.Group => RequestMode.Group,
        ScheduleTargetType.Room => RequestMode.Room,
        _ => throw new ArgumentOutOfRangeException(nameof(targetType)),
    };

    private static PsRozkladExport EnsureSuccessful(PsRozkladExport export)
    {
        if (!export.IsSuccess)
        {
            throw new PsRozkladApiException(export);
        }

        return export;
    }

    private static IReadOnlyList<BuildingDto> MapBuildings(PsRozkladExport export) =>
        (export.Blocks ?? [])
            .Where(b => b.Objects is { Count: > 0 })
            .Select(b => new BuildingDto
            {
                Name = b.Name ?? string.Empty,
                Rooms = MapEntities(b.Objects),
            })
            .ToList();

    private static IReadOnlyList<DepartmentDto> MapDepartments(PsRozkladExport export) =>
        (export.Departments ?? [])
            .Where(d => d.Objects is { Count: > 0 })
            .Select(d => new DepartmentDto
            {
                Name = d.Name ?? string.Empty,
                Entities = MapEntities(d.Objects),
            })
            .ToList();

    private static IReadOnlyList<ScheduleEntityDto> MapEntities(IReadOnlyList<ScheduleEntity>? entities) =>
        (entities ?? [])
            .Select(e => new ScheduleEntityDto
            {
                Id = e.Id ?? string.Empty,
                Name = e.Name ?? string.Empty,
                LastName = e.LastName,
                FirstName = e.FirstName,
                Patronymic = e.Patronymic,
                FullName = BuildTeacherFullName(e),
            })
            .ToList();

    private static LessonDto MapLesson(ScheduleItem item)
    {
        string description = item.LessonDescription ?? string.Empty;
        bool substitution = description.Contains("Увага! Заміна", StringComparison.OrdinalIgnoreCase);
        bool halfLesson = description.Contains("півпара", StringComparison.OrdinalIgnoreCase);

        return new LessonDto
        {
            ObjectName = item.ObjectName ?? string.Empty,
            Date = ScheduleDateExtensions.ParseApiDate(item.Date),
            Comment = item.Comment ?? string.Empty,
            LessonNumber = item.LessonNumber ?? string.Empty,
            LessonName = item.LessonName ?? string.Empty,
            LessonTime = item.LessonTime ?? string.Empty,
            LessonDescription = description,
            Half = item.Half ?? string.Empty,
            Teacher = item.Teacher ?? string.Empty,
            AdditionalTeachers = item.AdditionalTeachers ?? string.Empty,
            Room = item.Room ?? string.Empty,
            Group = item.Group ?? string.Empty,
            Title = item.Title ?? string.Empty,
            Type = item.Type ?? string.Empty,
            Replacement = item.Replacement ?? string.Empty,
            Reservation = item.Reservation ?? string.Empty,
            Online = item.Online ?? string.Empty,
            LinkComment = item.LinkComment ?? string.Empty,
            Link = item.Link ?? string.Empty,
            LessonType = GetLessonType(description),
            HourType = GetHourType(description),
            IsHalfLesson = halfLesson,
            IsSubstitution = substitution,
            LessonHours = substitution ? 0 : halfLesson ? 1 : 2,
        };
    }

    private static string BuildTeacherFullName(ScheduleEntity entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.LastName))
        {
            return $"{entity.LastName} {entity.FirstName} {entity.Patronymic}".Replace("  ", " ").Trim();
        }

        return entity.Name ?? string.Empty;
    }

    private static LessonType GetLessonType(string description)
    {
        if (description.Contains("(Лаб)", StringComparison.OrdinalIgnoreCase)) return LessonType.Laboratory;
        if (description.Contains("(Пр)", StringComparison.OrdinalIgnoreCase)) return LessonType.Practical;
        if (description.Contains("(Сем)", StringComparison.OrdinalIgnoreCase)) return LessonType.Seminar;
        if (description.Contains("(Зал)", StringComparison.OrdinalIgnoreCase)) return LessonType.Credit;
        if (description.Contains("(КЕкз)", StringComparison.OrdinalIgnoreCase)) return LessonType.ExamConsultation;
        if (description.Contains("(Екз)", StringComparison.OrdinalIgnoreCase)) return LessonType.Exam;
        if (description.Contains("(Л)", StringComparison.OrdinalIgnoreCase)) return LessonType.Lecture;

        return LessonType.Other;
    }

    private static HourType GetHourType(string description)
    {
        if (description.Contains("мз-", StringComparison.OrdinalIgnoreCase)) return HourType.Hourly;
        if (description.Contains("з-", StringComparison.OrdinalIgnoreCase)) return HourType.PartTime;

        return HourType.FullTime;
    }
}
