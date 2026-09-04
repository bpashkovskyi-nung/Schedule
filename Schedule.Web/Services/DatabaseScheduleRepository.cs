using Dekanat.ScheduleSdk.Enums;
using Dekanat.ScheduleSdk.Models;
using Microsoft.EntityFrameworkCore;
using Schedule.Web.Database;

namespace Schedule.Web.Services;

public sealed class DatabaseScheduleRepository(IDbContextFactory<ScheduleDbContext> dbFactory) : IScheduleRepository
{
    public bool SupportsDirectIcalExport => false;

    public async Task<PsRozkladExport> GetObjectListAsync(
        RequestMode mode,
        CancellationToken cancellationToken = default)
    {
        await using ScheduleDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);
        string targetType = ToTargetType(mode);

        if (mode is RequestMode.Teacher or RequestMode.Group)
        {
            List<ScheduleDepartmentRecord> departments = await db.Departments
                .AsNoTracking()
                .Where(x => x.TargetType == targetType)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);

            List<ScheduleEntityRecord> entities = await db.Entities
                .AsNoTracking()
                .Where(x => x.TargetType == targetType)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);

            return new PsRozkladExport
            {
                Code = 0,
                Departments = departments.Select(department => new Department
                {
                    Name = department.Name,
                    Objects = entities
                        .Where(entity => entity.DepartmentId == department.Id)
                        .Select(MapEntity)
                        .ToList(),
                }).ToList(),
            };
        }

        List<ScheduleBuildingRecord> buildings = await db.Buildings
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        List<ScheduleRoomRecord> rooms = await db.Rooms
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return new PsRozkladExport
        {
            Code = 0,
            Blocks = buildings.Select(building => new Building
            {
                Name = building.Name,
                Objects = rooms
                    .Where(room => room.BuildingId == building.Id)
                    .Select(room => new ScheduleEntity
                    {
                        Id = room.ExternalId,
                        Name = room.Name,
                    })
                    .ToList(),
            }).ToList(),
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
        string targetType = ToTargetType(mode);
        List<ScheduleLessonRecord> lessons = await db.Lessons
            .AsNoTracking()
            .Where(x => x.TargetType == targetType &&
                        x.ObjectId == objectId &&
                        x.Date >= beginDate &&
                        x.Date <= endDate)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.LessonNumber)
            .ToListAsync(cancellationToken);

        PsRozkladExport export = new()
        {
            Code = 0,
            ScheduleItems = lessons.Select(MapLesson).ToList(),
        };

        ScheduleItemDescription.Normalize(export.ScheduleItems);
        return export;
    }

    private static string ToTargetType(RequestMode mode) => mode switch
    {
        RequestMode.Teacher => "teacher",
        RequestMode.Group => "group",
        RequestMode.Room => "room",
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    private static ScheduleEntity MapEntity(ScheduleEntityRecord entity) => new()
    {
        Id = entity.ExternalId,
        Name = entity.Name,
        LastName = entity.LastName,
        FirstName = entity.FirstName,
        Patronymic = entity.Patronymic,
    };

    private static ScheduleItem MapLesson(ScheduleLessonRecord lesson) => new()
    {
        ObjectName = lesson.ObjectName,
        Date = lesson.Date.ToString("dd.MM.yyyy"),
        Comment = lesson.Comment,
        LessonNumber = lesson.LessonNumber,
        LessonName = lesson.LessonName,
        LessonTime = lesson.LessonTime,
        LessonDescription = lesson.LessonDescription,
        Half = lesson.Half,
        Teacher = lesson.Teacher,
        AdditionalTeachers = lesson.AdditionalTeachers,
        Room = lesson.Room,
        Group = lesson.Group,
        Title = lesson.Title,
        Type = lesson.Type,
        Replacement = lesson.Replacement,
        Reservation = lesson.Reservation,
        Online = lesson.Online,
        LinkComment = lesson.LinkComment,
        Link = lesson.Link,
    };
}
