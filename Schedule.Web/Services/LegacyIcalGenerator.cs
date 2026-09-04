using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dekanat.ScheduleSdk;
using Dekanat.ScheduleSdk.Models;

namespace Schedule.Web.Services;

public sealed class LegacyIcalGenerator : ILegacyIcalGenerator
{
    private const string DateTimeFormat = "yyyyMMdd'T'HHmmss'Z'";

    public byte[] Generate(PsRozkladExport export, string calendarName)
    {
        TimeZoneInfo timeZone = ResolveKyivTimeZone();
        DateTime generatedAt = DateTime.UtcNow;
        StringBuilder calendar = new();

        AppendLine(calendar, "BEGIN:VCALENDAR");
        AppendLine(calendar, "VERSION:2.0");
        AppendLine(calendar, "PRODID:-//NUNG//Schedule Legacy//UK");
        AppendLine(calendar, "CALSCALE:GREGORIAN");
        AppendLine(calendar, "METHOD:PUBLISH");
        AppendProperty(calendar, "X-WR-CALNAME", calendarName);

        IReadOnlyList<ScheduleItem> items = export.ScheduleItems ?? [];
        for (int index = 0; index < items.Count; index++)
        {
            ScheduleItem item = items[index];
            DateOnly? date = ScheduleDateExtensions.ParseApiDate(item.Date);
            if (date is null || string.IsNullOrWhiteSpace(item.LessonDescription))
            {
                continue;
            }

            string summary = FirstNonEmpty(item.Title, item.LessonDescription, item.LessonName, "Заняття");
            string description = BuildDescription(item);

            AppendLine(calendar, "BEGIN:VEVENT");
            AppendProperty(calendar, "UID", CreateUid(calendarName, index, item, date.Value));
            AppendProperty(calendar, "DTSTAMP", generatedAt.ToString(DateTimeFormat, CultureInfo.InvariantCulture));

            if (TryParseTimeRange(item.LessonTime, out TimeOnly start, out TimeOnly end) &&
                TryConvertToUtc(date.Value, start, end, timeZone, out DateTime startUtc, out DateTime endUtc))
            {
                AppendProperty(calendar, "DTSTART", startUtc.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
                AppendProperty(calendar, "DTEND", endUtc.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
            }
            else
            {
                AppendProperty(calendar, "DTSTART;VALUE=DATE", date.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                AppendProperty(
                    calendar,
                    "DTEND;VALUE=DATE",
                    date.Value.AddDays(1).ToString("yyyyMMdd", CultureInfo.InvariantCulture));
            }

            AppendProperty(calendar, "SUMMARY", summary);
            AppendProperty(calendar, "DESCRIPTION", description);
            AppendProperty(calendar, "LOCATION", item.Room);
            AppendProperty(calendar, "SEQUENCE", "0");
            AppendProperty(calendar, "STATUS", "CONFIRMED");
            AppendLine(calendar, "END:VEVENT");
        }

        AppendLine(calendar, "END:VCALENDAR");
        return Encoding.UTF8.GetBytes(calendar.ToString());
    }

    private static string BuildDescription(ScheduleItem item)
    {
        List<string> details = [];
        AddDetail(details, "Опис", item.LessonDescription);
        AddDetail(details, "Викладач", item.Teacher);
        AddDetail(details, "Додаткові викладачі", item.AdditionalTeachers);
        AddDetail(details, "Група", item.Group);
        AddDetail(details, "Аудиторія", item.Room);
        AddDetail(details, "Онлайн", item.Online);
        AddDetail(details, "Коментар до посилання", item.LinkComment);
        AddDetail(details, "Посилання", item.Link);
        AddDetail(details, "Заміна", item.Replacement);
        AddDetail(details, "Коментар", item.Comment);

        return string.Join("\n", details);
    }

    private static void AddDetail(ICollection<string> details, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            details.Add($"{label}: {value.Trim()}");
        }
    }

    private static string CreateUid(string calendarName, int index, ScheduleItem item, DateOnly date)
    {
        string source = string.Join(
            "|",
            calendarName,
            index.ToString(CultureInfo.InvariantCulture),
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            item.LessonNumber,
            item.LessonDescription,
            item.LessonTime,
            item.Room);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return $"{Convert.ToHexString(hash)[..24].ToLowerInvariant()}@schedule.nung.edu.ua";
    }

    private static bool TryParseTimeRange(
        string? value,
        out TimeOnly start,
        out TimeOnly end)
    {
        start = default;
        end = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string[] parts = value.Split(['-', '–', '—'], 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 &&
               TimeOnly.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out start) &&
               TimeOnly.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out end) &&
               end > start;
    }

    private static bool TryConvertToUtc(
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        TimeZoneInfo timeZone,
        out DateTime startUtc,
        out DateTime endUtc)
    {
        DateTime localStart = date.ToDateTime(start, DateTimeKind.Unspecified);
        DateTime localEnd = date.ToDateTime(end, DateTimeKind.Unspecified);

        if (timeZone.IsInvalidTime(localStart) || timeZone.IsInvalidTime(localEnd))
        {
            startUtc = default;
            endUtc = default;
            return false;
        }

        startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);
        return true;
    }

    private static TimeZoneInfo ResolveKyivTimeZone()
    {
        foreach (string timeZoneId in new[] { "Europe/Kyiv", "Europe/Kiev", "FLE Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the next platform-specific identifier.
            }
            catch (InvalidTimeZoneException)
            {
                // Try the next platform-specific identifier.
            }
        }

        return TimeZoneInfo.Utc;
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "Заняття";

    private static void AppendProperty(StringBuilder calendar, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            AppendLine(calendar, $"{name}:{EscapeText(value)}");
        }
    }

    private static string EscapeText(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace('\r', '\n')
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace(";", "\\;", StringComparison.Ordinal)
        .Replace(",", "\\,", StringComparison.Ordinal);

    private static void AppendLine(StringBuilder calendar, string value) =>
        calendar.Append(value).Append("\r\n");
}
