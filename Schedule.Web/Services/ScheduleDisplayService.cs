using Dekanat.ScheduleSdk;
using Dekanat.ScheduleSdk.Models;
using Schedule.Web.ViewModels;
using System.Text.RegularExpressions;

namespace Schedule.Web.Services;

internal static class ScheduleDisplayService
{
    internal static IReadOnlyDictionary<string, IReadOnlyList<ScheduleItemVm>> GroupByDate(
        IReadOnlyList<ScheduleItem>? items)
    {
        if (items is null || items.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<ScheduleItemVm>>();
        }

        return items
            .GroupBy(i => i.Date ?? string.Empty)
            .OrderBy(g => ParseApiDate(g.Key))
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ScheduleItemVm>)g.Select(MapItem).ToList());
    }

    internal static IReadOnlyList<WeekScheduleVm> CreateWeeklyView(
        IReadOnlyDictionary<string, IReadOnlyList<ScheduleItemVm>> grouped)
    {
        List<string> sortedDates = grouped.Keys.OrderBy(ParseApiDate).ToList();
        if (sortedDates.Count == 0)
        {
            return [];
        }

        DateOnly firstDate = ParseApiDate(sortedDates[0]);
        DateOnly lastDate = ParseApiDate(sortedDates[^1]);
        DateOnly firstMonday = GetWeekStart(firstDate);
        DateOnly lastFriday = GetWeekEnd(GetWeekStart(lastDate));

        List<WeekScheduleVm> weeks = [];
        DateOnly currentWeekStart = firstMonday;
        int weekNumber = 1;

        while (currentWeekStart <= lastFriday)
        {
            DateOnly weekEnd = currentWeekStart.AddDays(4);
            List<DayScheduleVm> days = [];

            for (int i = 0; i < 5; i++)
            {
                DateOnly dayDate = currentWeekStart.AddDays(i);
                string dayString = FormatApiDate(dayDate);
                IReadOnlyList<ScheduleItemVm> dayLessons = grouped.TryGetValue(dayString, out IReadOnlyList<ScheduleItemVm>? lessons)
                    ? lessons
                    : [];

                days.Add(new DayScheduleVm
                {
                    Date = dayString,
                    DayName = GetDayName(dayDate),
                    Lessons = dayLessons
                        .OrderBy(l => l.LessonTime, StringComparer.Ordinal)
                        .ToList(),
                });
            }

            weeks.Add(new WeekScheduleVm
            {
                WeekNumber = weekNumber++,
                StartDate = FormatApiDate(currentWeekStart),
                EndDate = FormatApiDate(weekEnd),
                Days = days,
            });

            currentWeekStart = currentWeekStart.AddDays(7);
        }

        return weeks;
    }

    internal static TeachingLoadResultVm BuildTeachingLoad(
        IReadOnlyList<ScheduleEntityVm> teachers,
        IReadOnlyList<PsRozkladExport> schedules)
    {
        Dictionary<string, bool> sessionTypes = new(StringComparer.Ordinal);
        Dictionary<string, Dictionary<string, int>> teacherWorkload = new(StringComparer.Ordinal);

        for (int i = 0; i < teachers.Count; i++)
        {
            string teacherId = teachers[i].Id;
            teacherWorkload[teacherId] = new Dictionary<string, int>(StringComparer.Ordinal);
            IReadOnlyList<ScheduleItem>? items = schedules[i].ScheduleItems;
            if (items is null)
            {
                continue;
            }

            foreach (ScheduleItem item in items)
            {
                string? sessionType = ExtractSessionType(item.LessonDescription);
                if (sessionType is null)
                {
                    continue;
                }

                sessionTypes.TryAdd(sessionType, true);
                if (!teacherWorkload[teacherId].TryGetValue(sessionType, out int hours))
                {
                    teacherWorkload[teacherId][sessionType] = 0;
                }

                teacherWorkload[teacherId][sessionType] = hours + 2;
            }
        }

        List<string> columns = sessionTypes.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        List<TeachingLoadRowVm> rows = teachers
            .Select(t =>
            {
                Dictionary<string, int> byType = teacherWorkload.GetValueOrDefault(t.Id) ?? new();
                int total = columns.Sum(c => byType.GetValueOrDefault(c));
                return new TeachingLoadRowVm
                {
                    TeacherName = t.FullName,
                    HoursByType = columns.ToDictionary(c => c, c => byType.GetValueOrDefault(c)),
                    TotalHours = total,
                };
            })
            .ToList();

        return new TeachingLoadResultVm { SessionTypes = columns, Rows = rows };
    }

    internal static int CountWorkingDays(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            return 0;
        }

        int count = 0;
        for (DateOnly cursor = from; cursor <= to; cursor = cursor.AddDays(1))
        {
            if (cursor.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                count++;
            }
        }

        return count;
    }

    internal static int CountActualPairs(
        IReadOnlyList<ScheduleItem>? items,
        int pairsFrom,
        int pairsTo)
    {
        if (items is null || items.Count == 0)
        {
            return 0;
        }

        HashSet<string> usedSlots = new(StringComparer.Ordinal);
        foreach (ScheduleItem item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Date) ||
                !int.TryParse(item.LessonNumber, out int lessonNumber) ||
                string.IsNullOrWhiteSpace(item.LessonDescription))
            {
                continue;
            }

            if (lessonNumber >= pairsFrom && lessonNumber <= pairsTo)
            {
                usedSlots.Add($"{item.Date}|{lessonNumber}");
            }
        }

        return usedSlots.Count;
    }

    private static ScheduleItemVm MapItem(ScheduleItem item) => new()
    {
        LessonNumber = item.LessonNumber ?? string.Empty,
        LessonTime = item.LessonTime ?? string.Empty,
        LessonDescription = item.LessonDescription ?? string.Empty,
        Links = ParseLinks(item.Link, item.LinkComment),
    };

    private static IReadOnlyList<ScheduleLinkVm> ParseLinks(string? value, string? comment)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        List<string> urls = Regex.Matches(value, @"https?://[^\s<>]+", RegexOptions.IgnoreCase)
            .Select(match => TrimUrl(match.Value))
            .Where(IsHttpUrl)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string label = string.IsNullOrWhiteSpace(comment) ? "Онлайн-заняття" : comment.Trim();
        return urls
            .Select((url, index) => new ScheduleLinkVm
            {
                Url = url,
                Label = urls.Count == 1 ? label : $"{label} {index + 1}",
            })
            .ToList();
    }

    private static string TrimUrl(string value) =>
        value.TrimEnd('.', ',', ';', ':', ')', ']', '}', '"', '\'');

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string? ExtractSessionType(string? lessonDescription)
    {
        if (string.IsNullOrWhiteSpace(lessonDescription))
        {
            return null;
        }

        string[] patterns = ["(Лаб)", "(Л)", "(Пр)", "(Сем)", "(КЗ)", "(Зал)", "(Екз)", "(Курс)", "(Диплом)", "(Конс)"];
        foreach (string pattern in patterns)
        {
            if (lessonDescription.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return pattern;
            }
        }

        return null;
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        int offset = date.DayOfWeek == DayOfWeek.Sunday ? -6 : 1 - (int)date.DayOfWeek;
        return date.AddDays(offset);
    }

    private static DateOnly GetWeekEnd(DateOnly weekStart) => weekStart.AddDays(6);

    private static string GetDayName(DateOnly date) => date.DayOfWeek switch
    {
        DayOfWeek.Monday => "Понеділок",
        DayOfWeek.Tuesday => "Вівторок",
        DayOfWeek.Wednesday => "Середа",
        DayOfWeek.Thursday => "Четвер",
        DayOfWeek.Friday => "П'ятниця",
        DayOfWeek.Saturday => "Субота",
        DayOfWeek.Sunday => "Неділя",
        _ => string.Empty,
    };

    private static DateOnly ParseApiDate(string? apiDate) =>
        ScheduleDateExtensions.ParseApiDate(apiDate) ?? DateOnly.MinValue;

    private static string FormatApiDate(DateOnly date) => date.ToString("dd.MM.yyyy");
}
