using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dekanat.ScheduleSdk;
using Dekanat.ScheduleSdk.Enums;
using Dekanat.ScheduleSdk.Exceptions;
using Dekanat.ScheduleSdk.Models;
using Dekanat.ScheduleSdk.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Schedule.Api.Configuration;
using Schedule.Api.Database;

namespace Schedule.Api.Services;

internal sealed class DekanatScheduleSynchronizer(
    IPsRozkladClient client,
    IDbContextFactory<ScheduleDbContext> dbFactory,
    IOptions<ScheduleSyncOptions> options,
    ILogger<DekanatScheduleSynchronizer> logger) : IScheduleSynchronizer
{
    public async Task SynchronizeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        if (toDate < fromDate)
        {
            throw new ArgumentException("The synchronization end date cannot be before the start date.", nameof(toDate));
        }

        long syncRunId = await StartSyncRunAsync(fromDate, toDate, cancellationToken);

        try
        {
            ReferenceSnapshot references = await LoadReferencesAsync(cancellationToken);
            if (references.Groups.Count == 0 || references.Teachers.Count == 0 || references.Rooms.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Dekanat reference data is incomplete: groups={references.Groups.Count}, " +
                    $"teachers={references.Teachers.Count}, rooms={references.Rooms.Count}.");
            }

            ConcurrentDictionary<string, LessonDraft> lessons = await LoadLessonsAsync(
                references,
                fromDate,
                toDate,
                cancellationToken);

            await SaveSnapshotAsync(
                references,
                lessons.Values,
                syncRunId,
                fromDate,
                toDate,
                cancellationToken);

            await CompleteSyncRunAsync(syncRunId, cancellationToken);
            logger.LogInformation(
                "Schedule synchronization completed for {FromDate:yyyy-MM-dd}..{ToDate:yyyy-MM-dd}: {LessonCount} canonical lessons.",
                fromDate,
                toDate,
                lessons.Count);
        }
        catch (Exception exception)
        {
            await FailSyncRunAsync(syncRunId, exception, cancellationToken);
            throw;
        }
    }

    private async Task<ReferenceSnapshot> LoadReferencesAsync(CancellationToken cancellationToken)
    {
        Task<PsRozkladExport> groupTask = client.GetObjectListAsync(new ObjectListRequest
        {
            Mode = RequestMode.Group,
            IncludeIds = true,
        }, cancellationToken);

        Task<PsRozkladExport> teacherTask = client.GetObjectListAsync(new ObjectListRequest
        {
            Mode = RequestMode.Teacher,
            IncludeIds = true,
        }, cancellationToken);

        Task<PsRozkladExport> roomTask = client.GetObjectListAsync(new ObjectListRequest
        {
            Mode = RequestMode.Room,
            IncludeIds = true,
        }, cancellationToken);

        await Task.WhenAll(groupTask, teacherTask, roomTask);

        return new ReferenceSnapshot(
            ReadEntities(groupTask.Result.Departments, "group"),
            ReadEntities(teacherTask.Result.Departments, "teacher"),
            ReadRooms(roomTask.Result.Blocks));
    }

    private async Task<ConcurrentDictionary<string, LessonDraft>> LoadLessonsAsync(
        ReferenceSnapshot references,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        ConcurrentDictionary<string, LessonDraft> lessons = new(StringComparer.Ordinal);
        using SemaphoreSlim concurrency = new(Math.Max(1, options.Value.RequestConcurrency));

        Task[] requests = references.Groups.Select(async group =>
        {
            await concurrency.WaitAsync(cancellationToken);
            try
            {
                PsRozkladExport export;
                try
                {
                    export = await client.GetScheduleAsync(new ScheduleRequest
                    {
                        Mode = RequestMode.Group,
                        ObjectId = group.ExternalId,
                        BeginDate = fromDate,
                        EndDate = toDate,
                        TextFormat = ScheduleTextFormat.Separated,
                        IncludeAllStreamComponents = true,
                    }, cancellationToken);
                }
                catch (PsRozkladApiException exception) when (exception.ErrorCode == -4)
                {
                    logger.LogWarning(
                        "Dekanat returned no schedule for group {GroupName} ({GroupId}) in {FromDate:yyyy-MM-dd}..{ToDate:yyyy-MM-dd}; skipping it.",
                        group.Name,
                        group.ExternalId,
                        fromDate,
                        toDate);
                    return;
                }

                ScheduleItemDescription.Normalize(export.ScheduleItems);
                foreach (ScheduleItem item in export.ScheduleItems ?? [])
                {
                    if (!HasMeaningfulContent(item) || item.GetDate() is null)
                    {
                        continue;
                    }

                    string sourceKey = BuildSourceKey(item);
                    LessonDraft lesson = lessons.GetOrAdd(sourceKey, _ => new LessonDraft(sourceKey, item));
                    lock (lesson)
                    {
                        lesson.AddGroup(group.ExternalId, ExtractSubgroup(item.Group, group.Name));
                    }
                }
            }
            finally
            {
                concurrency.Release();
            }
        }).ToArray();

        await Task.WhenAll(requests);
        return lessons;
    }

    private async Task SaveSnapshotAsync(
        ReferenceSnapshot references,
        IEnumerable<LessonDraft> lessons,
        long syncRunId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        await using ScheduleDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        Dictionary<string, int> departments = await UpsertDepartmentsAsync(db, references, cancellationToken);
        Dictionary<string, long> groups = await UpsertGroupsAsync(db, references.Groups, departments, cancellationToken);
        Dictionary<string, long> teachers = await UpsertTeachersAsync(db, references.Teachers, departments, cancellationToken);
        Dictionary<string, long> rooms = await UpsertRoomsAsync(db, references.Rooms, cancellationToken);
        Dictionary<string, long> teacherAliases = BuildTeacherAliases(references.Teachers, teachers);
        Dictionary<string, long> roomAliases = BuildRoomAliases(references.Rooms, rooms);

        await db.Lessons
            .Where(lesson => lesson.Date >= fromDate && lesson.Date <= toDate)
            .ExecuteDeleteAsync(cancellationToken);

        db.ChangeTracker.AutoDetectChangesEnabled = false;
        foreach (LessonDraft draft in lessons.OrderBy(x => x.Item.GetDate()).ThenBy(x => x.Item.LessonNumber))
        {
            ScheduleLessonRecord lesson = MapLesson(draft, syncRunId, groups, teacherAliases, roomAliases);
            db.Lessons.Add(lesson);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, int>> UpsertDepartmentsAsync(
        ScheduleDbContext db,
        ReferenceSnapshot references,
        CancellationToken cancellationToken)
    {
        List<ScheduleDepartmentRecord> existing = await db.Departments.ToListAsync(cancellationToken);
        Dictionary<string, ScheduleDepartmentRecord> byKey = existing.ToDictionary(
            department => DepartmentKey(department.TargetType, department.Name),
            StringComparer.OrdinalIgnoreCase);

        foreach (ReferenceEntity entity in references.Groups.Concat(references.Teachers))
        {
            string key = DepartmentKey(entity.TargetType, entity.DepartmentName);
            if (!byKey.TryGetValue(key, out ScheduleDepartmentRecord? department))
            {
                department = new ScheduleDepartmentRecord
                {
                    TargetType = entity.TargetType,
                    Name = entity.DepartmentName,
                };
                db.Departments.Add(department);
                byKey.Add(key, department);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return byKey.ToDictionary(x => x.Key, x => x.Value.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, long>> UpsertGroupsAsync(
        ScheduleDbContext db,
        IReadOnlyList<ReferenceEntity> source,
        IReadOnlyDictionary<string, int> departments,
        CancellationToken cancellationToken)
    {
        List<ScheduleGroupRecord> existing = await db.Groups.ToListAsync(cancellationToken);
        Dictionary<string, ScheduleGroupRecord> byId = existing.ToDictionary(
            group => group.ExternalId,
            StringComparer.OrdinalIgnoreCase);

        foreach (ReferenceEntity item in source)
        {
            if (!byId.TryGetValue(item.ExternalId, out ScheduleGroupRecord? group))
            {
                group = new ScheduleGroupRecord { ExternalId = item.ExternalId };
                db.Groups.Add(group);
                byId.Add(item.ExternalId, group);
            }

            group.Name = item.Name;
            group.DepartmentId = departments[DepartmentKey(item.TargetType, item.DepartmentName)];
            group.SortOrder = item.SortOrder;
        }

        await db.SaveChangesAsync(cancellationToken);
        return byId.ToDictionary(x => x.Key, x => x.Value.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, long>> UpsertTeachersAsync(
        ScheduleDbContext db,
        IReadOnlyList<ReferenceEntity> source,
        IReadOnlyDictionary<string, int> departments,
        CancellationToken cancellationToken)
    {
        List<ScheduleTeacherRecord> existing = await db.Teachers.ToListAsync(cancellationToken);
        Dictionary<string, ScheduleTeacherRecord> byId = existing.ToDictionary(
            teacher => teacher.ExternalId,
            StringComparer.OrdinalIgnoreCase);

        foreach (ReferenceEntity item in source)
        {
            if (!byId.TryGetValue(item.ExternalId, out ScheduleTeacherRecord? teacher))
            {
                teacher = new ScheduleTeacherRecord { ExternalId = item.ExternalId };
                db.Teachers.Add(teacher);
                byId.Add(item.ExternalId, teacher);
            }

            teacher.Name = item.Name;
            teacher.LastName = item.LastName;
            teacher.FirstName = item.FirstName;
            teacher.Patronymic = item.Patronymic;
            teacher.DepartmentId = departments[DepartmentKey(item.TargetType, item.DepartmentName)];
            teacher.SortOrder = item.SortOrder;
        }

        await db.SaveChangesAsync(cancellationToken);
        return byId.ToDictionary(x => x.Key, x => x.Value.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, long>> UpsertRoomsAsync(
        ScheduleDbContext db,
        IReadOnlyList<ReferenceRoom> source,
        CancellationToken cancellationToken)
    {
        List<ScheduleBuildingRecord> existingBuildings = await db.Buildings.ToListAsync(cancellationToken);
        Dictionary<string, ScheduleBuildingRecord> buildings = existingBuildings.ToDictionary(
            building => building.Name,
            StringComparer.OrdinalIgnoreCase);

        foreach (ReferenceRoom item in source)
        {
            if (!buildings.TryGetValue(item.BuildingName, out ScheduleBuildingRecord? building))
            {
                building = new ScheduleBuildingRecord { Name = item.BuildingName };
                db.Buildings.Add(building);
                buildings.Add(item.BuildingName, building);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        List<ScheduleRoomRecord> existingRooms = await db.Rooms.ToListAsync(cancellationToken);
        Dictionary<string, ScheduleRoomRecord> rooms = existingRooms.ToDictionary(
            room => room.ExternalId,
            StringComparer.OrdinalIgnoreCase);

        foreach (ReferenceRoom item in source)
        {
            if (!rooms.TryGetValue(item.ExternalId, out ScheduleRoomRecord? room))
            {
                room = new ScheduleRoomRecord { ExternalId = item.ExternalId };
                db.Rooms.Add(room);
                rooms.Add(item.ExternalId, room);
            }

            room.Name = item.Name;
            room.BuildingId = buildings[item.BuildingName].Id;
            room.SortOrder = item.SortOrder;
        }

        await db.SaveChangesAsync(cancellationToken);
        return rooms.ToDictionary(x => x.Key, x => x.Value.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static ScheduleLessonRecord MapLesson(
        LessonDraft draft,
        long syncRunId,
        IReadOnlyDictionary<string, long> groups,
        IReadOnlyDictionary<string, long> teacherAliases,
        IReadOnlyDictionary<string, long> roomAliases)
    {
        ScheduleItem item = draft.Item;
        ScheduleLessonRecord lesson = new()
        {
            SourceKey = draft.SourceKey,
            SourceHash = BuildFullHash(item),
            Date = item.GetDate() ?? throw new InvalidOperationException("Schedule item has no date."),
            Comment = item.Comment,
            LessonNumber = item.LessonNumber,
            LessonName = item.LessonName,
            LessonTime = item.LessonTime,
            LessonDescription = item.LessonDescription,
            Half = item.Half,
            Title = item.Title,
            Type = item.Type,
            Replacement = item.Replacement,
            Reservation = item.Reservation,
            Online = item.Online,
            LinkComment = item.LinkComment,
            Link = item.Link,
            SortOrder = ParseSortOrder(item.LessonNumber),
            LastSyncRunId = syncRunId,
        };

        foreach ((string externalId, HashSet<string> subgroups) in draft.Groups)
        {
            if (!groups.TryGetValue(externalId, out long groupId))
            {
                continue;
            }

            foreach (string subgroup in subgroups.DefaultIfEmpty(string.Empty))
            {
                lesson.Groups.Add(new ScheduleLessonGroupRecord
                {
                    Lesson = lesson,
                    GroupId = groupId,
                    Subgroup = subgroup,
                });
            }
        }

        AddTeachers(lesson, item.Teacher, true, teacherAliases);
        AddTeachers(lesson, item.AdditionalTeachers, false, teacherAliases);
        AddRooms(lesson, item.Room, roomAliases);
        return lesson;
    }

    private static void AddTeachers(
        ScheduleLessonRecord lesson,
        string? text,
        bool isPrimary,
        IReadOnlyDictionary<string, long> teacherAliases)
    {
        foreach (string value in SplitValues(text))
        {
            if (!TryFindReferenceId(value, teacherAliases, out long teacherId) ||
                lesson.Teachers.Any(link => link.TeacherId == teacherId))
            {
                continue;
            }

            lesson.Teachers.Add(new ScheduleLessonTeacherRecord
            {
                Lesson = lesson,
                TeacherId = teacherId,
                IsPrimary = isPrimary,
            });
        }
    }

    private static bool TryFindReferenceId(
        string value,
        IReadOnlyDictionary<string, long> aliases,
        out long id)
    {
        string normalized = NormalizeLookupText(value);
        if (aliases.TryGetValue(normalized, out id))
        {
            return true;
        }

        KeyValuePair<string, long>? suffixMatch = aliases
            .Where(alias => normalized.EndsWith(alias.Key, StringComparison.OrdinalIgnoreCase) ||
                            alias.Key.EndsWith(normalized, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(alias => alias.Key.Length)
            .Select(alias => (KeyValuePair<string, long>?)alias)
            .FirstOrDefault();

        if (suffixMatch is not null)
        {
            id = suffixMatch.Value.Value;
            return true;
        }

        id = default;
        return false;
    }

    private static void AddRooms(
        ScheduleLessonRecord lesson,
        string? text,
        IReadOnlyDictionary<string, long> roomAliases)
    {
        bool isPrimary = true;
        foreach (string value in SplitValues(text))
        {
            if (!roomAliases.TryGetValue(NormalizeLookupText(value), out long roomId) ||
                lesson.Rooms.Any(link => link.RoomId == roomId))
            {
                continue;
            }

            lesson.Rooms.Add(new ScheduleLessonRoomRecord
            {
                Lesson = lesson,
                RoomId = roomId,
                IsPrimary = isPrimary,
            });
            isPrimary = false;
        }
    }

    private static Dictionary<string, long> BuildTeacherAliases(
        IReadOnlyList<ReferenceEntity> source,
        IReadOnlyDictionary<string, long> ids)
    {
        Dictionary<string, long> aliases = new(StringComparer.OrdinalIgnoreCase);
        foreach (ReferenceEntity teacher in source)
        {
            if (!ids.TryGetValue(teacher.ExternalId, out long teacherId))
            {
                continue;
            }

            AddAlias(aliases, teacher.Name, teacherId);
            AddAlias(aliases, $"{teacher.LastName} {teacher.FirstName} {teacher.Patronymic}", teacherId);
            AddAlias(aliases, $"{teacher.LastName} {teacher.FirstName}", teacherId);
            if (!string.IsNullOrWhiteSpace(teacher.LastName) && !string.IsNullOrWhiteSpace(teacher.FirstName))
            {
                AddAlias(aliases, $"{teacher.LastName} {teacher.FirstName[0]}", teacherId);
            }
        }

        return aliases;
    }

    private static Dictionary<string, long> BuildRoomAliases(
        IReadOnlyList<ReferenceRoom> source,
        IReadOnlyDictionary<string, long> ids)
    {
        Dictionary<string, long> aliases = new(StringComparer.OrdinalIgnoreCase);
        foreach (ReferenceRoom room in source)
        {
            if (ids.TryGetValue(room.ExternalId, out long roomId))
            {
                AddAlias(aliases, room.Name, roomId);
            }
        }

        return aliases;
    }

    private static void AddAlias(Dictionary<string, long> aliases, string? value, long id)
    {
        string alias = NormalizeLookupText(value);
        if (!string.IsNullOrWhiteSpace(alias))
        {
            aliases.TryAdd(alias, id);
        }
    }

    private static IReadOnlyList<ReferenceEntity> ReadEntities(
        IReadOnlyList<Department>? departments,
        string targetType)
    {
        return (departments ?? [])
            .SelectMany((department, departmentIndex) => (department.Objects ?? [])
                .Where(entity => !string.IsNullOrWhiteSpace(entity.Id))
                .Select((entity, objectIndex) => new ReferenceEntity(
                    targetType,
                    entity.Id!,
                    entity.Name ?? string.Empty,
                    department.Name ?? string.Empty,
                    entity.LastName,
                    entity.FirstName,
                    entity.Patronymic,
                    departmentIndex * 10000 + objectIndex)))
            .GroupBy(entity => entity.ExternalId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private static IReadOnlyList<ReferenceRoom> ReadRooms(IReadOnlyList<Building>? buildings)
    {
        return (buildings ?? [])
            .SelectMany((building, buildingIndex) => (building.Objects ?? [])
                .Where(entity => !string.IsNullOrWhiteSpace(entity.Id))
                .Select((entity, roomIndex) => new ReferenceRoom(
                    entity.Id!,
                    entity.Name ?? string.Empty,
                    building.Name ?? string.Empty,
                    buildingIndex * 10000 + roomIndex)))
            .GroupBy(room => room.ExternalId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private static bool HasMeaningfulContent(ScheduleItem item) =>
        !string.IsNullOrWhiteSpace(item.LessonDescription) ||
        !string.IsNullOrWhiteSpace(item.Title) ||
        !string.IsNullOrWhiteSpace(item.LessonName) ||
        !string.IsNullOrWhiteSpace(item.Teacher) ||
        !string.IsNullOrWhiteSpace(item.Room) ||
        !string.IsNullOrWhiteSpace(item.Group) ||
        !string.IsNullOrWhiteSpace(item.Replacement) ||
        !string.IsNullOrWhiteSpace(item.Link);

    private static string BuildSourceKey(ScheduleItem item) => BuildHash([
        item.GetDate()?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        item.LessonNumber,
        item.LessonTime,
        item.LessonName,
        item.Title,
        item.Type,
        item.Half,
        item.Teacher,
        item.AdditionalTeachers,
        item.Room,
        item.Replacement,
        item.Reservation,
        item.Online,
        item.Link,
    ]);

    private static string BuildFullHash(ScheduleItem item) => BuildHash([
        item.Date,
        item.Comment,
        item.LessonNumber,
        item.LessonName,
        item.LessonTime,
        item.LessonDescription,
        item.Half,
        item.Teacher,
        item.AdditionalTeachers,
        item.Room,
        item.Group,
        item.Title,
        item.Type,
        item.Replacement,
        item.Reservation,
        item.Online,
        item.LinkComment,
        item.Link,
    ]);

    private static string BuildHash(IEnumerable<string?> values)
    {
        string value = string.Join('\u001f', values.Select(NormalizeKeyText));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static string NormalizeKeyText(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Trim()
            .ToUpperInvariant();

    private static string NormalizeLookupText(string? value)
    {
        StringBuilder builder = new();
        foreach (char character in value?.ToUpperInvariant() ?? string.Empty)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static IEnumerable<string> SplitValues(string? value) =>
        (value ?? string.Empty)
            .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item));

    private static string ExtractSubgroup(string? value, string groupName)
    {
        string subgroup = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(subgroup))
        {
            return string.Empty;
        }

        int groupIndex = subgroup.IndexOf(groupName, StringComparison.CurrentCultureIgnoreCase);
        if (groupIndex >= 0)
        {
            subgroup = subgroup.Remove(groupIndex, groupName.Length);
        }

        subgroup = subgroup.Trim(' ', ',', ';', '-', '—');
        return subgroup.Length <= 128 ? subgroup : string.Empty;
    }

    private static int ParseSortOrder(string? lessonNumber) =>
        int.TryParse(lessonNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
            ? number
            : 0;

    private static string DepartmentKey(string targetType, string name) =>
        $"{targetType}|{NormalizeLookupText(name)}";

    private async Task<long> StartSyncRunAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        await using ScheduleDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);
        ScheduleSyncRunRecord run = new()
        {
            FromDate = fromDate,
            ToDate = toDate,
            StartedAtUtc = DateTime.UtcNow,
            Status = "Running",
        };
        db.SyncRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
        return run.Id;
    }

    private async Task CompleteSyncRunAsync(long syncRunId, CancellationToken cancellationToken)
    {
        await using ScheduleDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);
        ScheduleSyncRunRecord? run = await db.SyncRuns.FindAsync([syncRunId], cancellationToken);
        if (run is null)
        {
            return;
        }

        run.Status = "Completed";
        run.CompletedAtUtc = DateTime.UtcNow;
        run.Error = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task FailSyncRunAsync(long syncRunId, Exception exception, CancellationToken cancellationToken)
    {
        try
        {
            await using ScheduleDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);
            ScheduleSyncRunRecord? run = await db.SyncRuns.FindAsync([syncRunId], cancellationToken);
            if (run is null)
            {
                return;
            }

            run.Status = "Failed";
            run.CompletedAtUtc = DateTime.UtcNow;
            run.Error = exception.ToString()[..Math.Min(4000, exception.ToString().Length)];
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception statusException)
        {
            logger.LogError(statusException, "Could not save failed schedule synchronization status for run {SyncRunId}.", syncRunId);
        }
    }

    private sealed record ReferenceSnapshot(
        IReadOnlyList<ReferenceEntity> Groups,
        IReadOnlyList<ReferenceEntity> Teachers,
        IReadOnlyList<ReferenceRoom> Rooms);

    private sealed record ReferenceEntity(
        string TargetType,
        string ExternalId,
        string Name,
        string DepartmentName,
        string? LastName,
        string? FirstName,
        string? Patronymic,
        int SortOrder);

    private sealed record ReferenceRoom(
        string ExternalId,
        string Name,
        string BuildingName,
        int SortOrder);

    private sealed class LessonDraft(string sourceKey, ScheduleItem item)
    {
        public string SourceKey { get; } = sourceKey;
        public ScheduleItem Item { get; } = item;
        public Dictionary<string, HashSet<string>> Groups { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void AddGroup(string externalId, string subgroup)
        {
            if (!Groups.TryGetValue(externalId, out HashSet<string>? values))
            {
                values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Groups.Add(externalId, values);
            }

            values.Add(subgroup);
        }
    }
}
