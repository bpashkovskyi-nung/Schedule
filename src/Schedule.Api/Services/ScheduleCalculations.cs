using Dekanat.ScheduleSdk.Models;
using Schedule.Contracts.AuditoriumLoad;
using Schedule.Contracts.ReferenceData;
using Schedule.Contracts.Workload;

namespace Schedule.Api.Services;

internal static class ScheduleCalculations
{
    public static TeachingLoadResponse BuildTeachingLoad(
        IReadOnlyList<ScheduleEntityDto> teachers,
        IReadOnlyList<PsRozkladExport> schedules)
    {
        Dictionary<string, Dictionary<string, int>> teacherWorkload = new(StringComparer.Ordinal);
        HashSet<string> sessionTypes = new(StringComparer.Ordinal);

        for (int i = 0; i < teachers.Count; i++)
        {
            string teacherId = teachers[i].Id;
            Dictionary<string, int> byType = new(StringComparer.Ordinal);
            teacherWorkload[teacherId] = byType;

            foreach (ScheduleItem item in schedules[i].ScheduleItems ?? [])
            {
                string? sessionType = ExtractSessionType(item.LessonDescription);
                if (sessionType is null) continue;

                sessionTypes.Add(sessionType);
                byType[sessionType] = byType.GetValueOrDefault(sessionType) + 2;
            }
        }

        List<string> columns = sessionTypes.OrderBy(x => x, StringComparer.Ordinal).ToList();
        List<TeachingLoadRowDto> rows = teachers.Select(teacher =>
        {
            Dictionary<string, int> byType = teacherWorkload.GetValueOrDefault(teacher.Id) ?? [];
            return new TeachingLoadRowDto
            {
                TeacherName = teacher.FullName,
                HoursByType = columns.ToDictionary(x => x, x => byType.GetValueOrDefault(x)),
                TotalHours = columns.Sum(x => byType.GetValueOrDefault(x)),
            };
        }).ToList();

        return new TeachingLoadResponse { SessionTypes = columns, Rows = rows };
    }

    public static int CountWorkingDays(DateOnly from, DateOnly to)
    {
        int count = 0;
        for (DateOnly date = from; date <= to; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) count++;
        }

        return count;
    }

    public static int CountActualPairs(IReadOnlyList<ScheduleItem>? items, int pairsFrom, int pairsTo)
    {
        HashSet<string> slots = new(StringComparer.Ordinal);
        foreach (ScheduleItem item in items ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.Date) ||
                !int.TryParse(item.LessonNumber, out int lessonNumber) ||
                string.IsNullOrWhiteSpace(item.LessonDescription))
            {
                continue;
            }

            if (lessonNumber >= pairsFrom && lessonNumber <= pairsTo)
            {
                slots.Add($"{item.Date}|{lessonNumber}");
            }
        }

        return slots.Count;
    }

    public static IReadOnlyList<AuditoriumLoadItemDto> SortLoadItems(IEnumerable<AuditoriumLoadItemDto> items) =>
        items.OrderByDescending(x => x.Percent).ThenBy(x => x.BuildingName).ThenBy(x => x.RoomName).ToList();

    private static string? ExtractSessionType(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;

        string[] patterns = ["(Лаб)", "(Л)", "(Пр)", "(Сем)", "(КЗ)", "(Зал)", "(Екз)", "(Курс)", "(Диплом)", "(Конс)"];
        return patterns.FirstOrDefault(pattern => description.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
