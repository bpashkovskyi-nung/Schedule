using System.Globalization;
using Dekanat.ScheduleSdk.Enums;
using Dekanat.ScheduleSdk.Models;
using Microsoft.EntityFrameworkCore;
using Schedule.Api.Database;

namespace Schedule.Api.Services;

internal sealed class DatabaseScheduleDataSource(
    IDbContextFactory<ScheduleDbContext> dbFactory) : IScheduleDataSource
{
    public bool IsDatabase => true;

    public async Task<PsRozkladExport> GetObjectListAsync(
        RequestMode mode,
        CancellationToken cancellationToken = default)
    {
        await using ScheduleDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

        if (mode == RequestMode.Teacher)
        {
            return new PsRozkladExport
            {
                Code = 0,
                Departments = await BuildDepartmentsAsync(
                    db,
                    "teacher",
                    db.Teachers,
                    MapTeacher,
                    cancellationToken),
            };
        }

        if (mode == RequestMode.Group)
        {
            return new PsRozkladExport
            {
                Code = 0,
                Departments = await BuildDepartmentsAsync(
                    db,
                    "group",
                    db.Groups,
                    MapGroup,
                    cancellationToken),
            };
        }

        List<ScheduleBuildingRecord> buildings = await db.Buildings
            .AsNoTracking()
            .Include(x => x.Rooms)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return new PsRozkladExport
        {
            Code = 0,
            Blocks = buildings
                .Select(building => new Building
                {
                    Name = building.Name,
                    Objects = building.Rooms
                        .OrderBy(room => room.Name)
                        .Select(room => new ScheduleEntity
                        {
                            Id = room.ExternalId,
                            Name = room.Name,
                        })
                        .ToList(),
                })
                .Where(building => building.Objects is { Count: > 0 })
                .ToList(),
        };
    }

    public async Task<PsRozkladExport> GetScheduleAsync(
        RequestMode mode,
        string objectId,
        DateOnly beginDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        await using ScheduleDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<long> lessonIds = mode switch
        {
            RequestMode.Teacher => db.LessonTeachers
                .Where(link => link.Teacher.ExternalId == objectId)
                .Select(link => link.LessonId),
            RequestMode.Group => db.LessonGroups
                .Where(link => link.Group.ExternalId == objectId)
                .Select(link => link.LessonId),
            RequestMode.Room => db.LessonRooms
                .Where(link => link.Room.ExternalId == objectId)
                .Select(link => link.LessonId),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };

        List<ScheduleLessonRecord> lessons = await db.Lessons
            .AsNoTracking()
            .AsSplitQuery()
            .Where(lesson => lessonIds.Contains(lesson.Id) &&
                             lesson.Date >= beginDate &&
                             lesson.Date <= endDate)
            .Include(lesson => lesson.Groups)
                .ThenInclude(link => link.Group)
            .Include(lesson => lesson.Teachers)
                .ThenInclude(link => link.Teacher)
            .Include(lesson => lesson.Rooms)
                .ThenInclude(link => link.Room)
            .OrderBy(lesson => lesson.Date)
            .ThenBy(lesson => lesson.SortOrder)
            .ThenBy(lesson => lesson.LessonNumber)
            .ToListAsync(cancellationToken);

        PsRozkladExport export = new()
        {
            Code = 0,
            ScheduleItems = lessons.Select(MapLesson).ToList(),
        };

        ScheduleItemDescription.Normalize(export.ScheduleItems);
        return export;
    }

    private static async Task<IReadOnlyList<Department>> BuildDepartmentsAsync<TRecord>(
        ScheduleDbContext db,
        string targetType,
        IQueryable<TRecord> records,
        Func<TRecord, ScheduleEntity> map,
        CancellationToken cancellationToken)
        where TRecord : class
    {
        List<ScheduleDepartmentRecord> departments = await db.Departments
            .AsNoTracking()
            .Where(department => department.TargetType == targetType)
            .OrderBy(department => department.Name)
            .ToListAsync(cancellationToken);

        List<TRecord> entities = await records
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return departments
            .Select(department => new Department
            {
                Name = department.Name,
                Objects = entities
                    .Where(entity => GetDepartmentId(entity) == department.Id)
                    .Select(map)
                    .OrderBy(entity => entity.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList(),
            })
            .Where(department => department.Objects is { Count: > 0 })
            .ToList();
    }

    private static int? GetDepartmentId<TRecord>(TRecord record) => record switch
    {
        ScheduleGroupRecord group => group.DepartmentId,
        ScheduleTeacherRecord teacher => teacher.DepartmentId,
        _ => throw new ArgumentOutOfRangeException(nameof(record)),
    };

    private static ScheduleEntity MapGroup(ScheduleGroupRecord group) => new()
    {
        Id = group.ExternalId,
        Name = group.Name,
    };

    private static ScheduleEntity MapTeacher(ScheduleTeacherRecord teacher) => new()
    {
        Id = teacher.ExternalId,
        Name = teacher.Name,
        LastName = teacher.LastName,
        FirstName = teacher.FirstName,
        Patronymic = teacher.Patronymic,
    };

    private static ScheduleItem MapLesson(ScheduleLessonRecord lesson) => new()
    {
        Date = lesson.Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
        Comment = lesson.Comment,
        LessonNumber = lesson.LessonNumber,
        LessonName = lesson.LessonName,
        LessonTime = lesson.LessonTime,
        LessonDescription = lesson.LessonDescription,
        Half = lesson.Half,
        Teacher = BuildTeacherText(lesson),
        AdditionalTeachers = BuildAdditionalTeacherText(lesson),
        Room = BuildRoomText(lesson),
        Group = BuildGroupText(lesson),
        Title = lesson.Title,
        Type = lesson.Type,
        Replacement = lesson.Replacement,
        Reservation = lesson.Reservation,
        Online = lesson.Online,
        LinkComment = lesson.LinkComment,
        Link = lesson.Link,
    };

    private static string? BuildTeacherText(ScheduleLessonRecord lesson) =>
        lesson.Teachers
            .OrderByDescending(link => link.IsPrimary)
            .ThenBy(link => link.Teacher.Name)
            .Select(link => TeacherDisplayName(link.Teacher))
            .FirstOrDefault();

    private static string? BuildAdditionalTeacherText(ScheduleLessonRecord lesson) =>
        string.Join(", ", lesson.Teachers
            .OrderByDescending(link => link.IsPrimary)
            .ThenBy(link => link.Teacher.Name)
            .Skip(1)
            .Select(link => TeacherDisplayName(link.Teacher)));

    private static string? BuildRoomText(ScheduleLessonRecord lesson) =>
        JoinDistinct(lesson.Rooms
            .OrderByDescending(link => link.IsPrimary)
            .ThenBy(link => link.Room.Name)
            .Select(link => link.Room.Name));

    private static string? BuildGroupText(ScheduleLessonRecord lesson) =>
        JoinDistinct(lesson.Groups
            .OrderBy(link => link.Group.Name)
            .Select(link => string.IsNullOrWhiteSpace(link.Subgroup)
                ? link.Group.Name
                : $"{link.Group.Name} {link.Subgroup}"));

    private static string TeacherDisplayName(ScheduleTeacherRecord teacher)
    {
        if (!string.IsNullOrWhiteSpace(teacher.LastName))
        {
            return $"{teacher.LastName} {teacher.FirstName} {teacher.Patronymic}".Replace("  ", " ").Trim();
        }

        return teacher.Name;
    }

    private static string? JoinDistinct(IEnumerable<string> values)
    {
        string[] result = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return result.Length == 0 ? null : string.Join(", ", result);
    }
}
