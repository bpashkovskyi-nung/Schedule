using System.Globalization;
using System.Text.RegularExpressions;
using Schedule.Contracts.Schedules;
using Schedule.Web.ViewModels;

namespace Schedule.Web.Services;

internal static class ScheduleDisplayService
{
    internal static IReadOnlyDictionary<string, IReadOnlyList<ScheduleItemVm>> GroupByDate(
        IReadOnlyList<LessonDto>? items)
    {
        if (items is null || items.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<ScheduleItemVm>>();
        }

        return items
            .Where(item => item.Date.HasValue && !string.IsNullOrWhiteSpace(item.LessonDescription))
            .GroupBy(item => item.Date!.Value)
            .OrderBy(group => group.Key)
            .ToDictionary(
                group => group.Key.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                group => (IReadOnlyList<ScheduleItemVm>)group.Select(MapItem).ToList());
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

            for (int offset = 0; offset < 5; offset++)
            {
                DateOnly dayDate = currentWeekStart.AddDays(offset);
                string dayString = dayDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
                IReadOnlyList<ScheduleItemVm> dayLessons = grouped.TryGetValue(dayString, out IReadOnlyList<ScheduleItemVm>? lessons)
                    ? lessons
                    : [];

                days.Add(new DayScheduleVm
                {
                    Date = dayString,
                    DayName = GetDayName(dayDate),
                    Lessons = dayLessons.OrderBy(lesson => lesson.LessonTime, StringComparer.Ordinal).ToList(),
                });
            }

            weeks.Add(new WeekScheduleVm
            {
                WeekNumber = weekNumber++,
                StartDate = currentWeekStart.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                EndDate = weekEnd.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                Days = days,
            });

            currentWeekStart = currentWeekStart.AddDays(7);
        }

        return weeks;
    }

    private static ScheduleItemVm MapItem(LessonDto item) => new()
    {
        LessonNumber = item.LessonNumber,
        LessonTime = item.LessonTime,
        LessonDescription = item.LessonDescription,
        Links = ParseLinks(item.Link, item.LinkComment),
    };

    private static IReadOnlyList<ScheduleLinkVm> ParseLinks(string? value, string? comment)
    {
        if (string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(comment))
        {
            return [];
        }

        string source = string.Join("\n", new[] { value, comment }.Where(item => !string.IsNullOrWhiteSpace(item)));
        List<string> urls = Regex.Matches(source, @"https?://[^\s<>;,""']+", RegexOptions.IgnoreCase)
            .Select(match => TrimUrl(match.Value))
            .Where(IsHttpUrl)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string label = GetLinkLabel(comment);
        return urls.Select((url, index) => new ScheduleLinkVm
        {
            Url = url,
            Label = urls.Count == 1 ? label : $"{label} {index + 1}",
        }).ToList();
    }

    private static string GetLinkLabel(string? comment) =>
        string.IsNullOrWhiteSpace(comment)
            ? "Онлайн-заняття"
            : Regex.Replace(comment, @"https?://[^\s<>;,""']+", string.Empty, RegexOptions.IgnoreCase)
                .Trim(' ', '\t', '\r', '\n', ':', ';', ',') is { Length: > 0 } label
                ? label
                : "Онлайн-заняття";

    private static string TrimUrl(string value) => value.TrimEnd('.', ',', ';', ':', ')', ']', '}', '"', '\'');

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static DateOnly ParseApiDate(string value) =>
        DateOnly.TryParseExact(value, ["dd.MM.yyyy", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
            ? date
            : DateOnly.MinValue;

    private static DateOnly GetWeekStart(DateOnly date)
    {
        int offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
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
}
