using System.Globalization;

namespace Schedule.Client.UI.Components;

public sealed class PeriodSelection
{
    public string PeriodKey { get; set; } = "to_end_of_week";
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(6));
    public bool WeeklyView { get; set; }

    public bool IsCustom => string.Equals(PeriodKey, "custom", StringComparison.OrdinalIgnoreCase);

    public void ApplyPeriod(string? periodKey)
    {
        PeriodKey = string.IsNullOrWhiteSpace(periodKey) ? "to_end_of_week" : periodKey;
        if (IsCustom)
        {
            return;
        }

        PeriodChoice? choice = GetOptions().FirstOrDefault(option => option.Key == PeriodKey);
        if (choice is not null)
        {
            FromDate = choice.FromDate;
            ToDate = choice.ToDate;
        }
    }

    public static IReadOnlyList<PeriodChoice> GetOptions()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        DateOnly monday = today.AddDays(-daysFromMonday);
        DateOnly sunday = monday.AddDays(6);
        DateOnly monthStart = new(today.Year, today.Month, 1);
        DateOnly monthEnd = monthStart.AddMonths(1).AddDays(-1);
        DateOnly previousMonthStart = monthStart.AddMonths(-1);
        DateOnly previousMonthEnd = monthStart.AddDays(-1);
        DateOnly termStart;
        DateOnly termEnd;

        if (today.Month >= 8)
        {
            termStart = new(today.Year, 9, 1);
            termEnd = new(today.Year, 12, 31);
        }
        else
        {
            termStart = new(today.Year, 2, 1);
            termEnd = new(today.Year, 6, 30);
        }

        return
        [
            new("to_end_of_week", "До кінця тижня", today, sunday),
            new("current_week", "Поточний тиждень", monday, sunday),
            new("current_month", "Поточний місяць", monthStart, monthEnd),
            new("previous_month", "Попередній місяць", previousMonthStart, previousMonthEnd),
            new("current_term", "Поточний семестр", termStart, termEnd),
            new("custom", "Власний період", today, today.AddDays(6)),
        ];
    }

    public static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

public sealed record PeriodChoice(string Key, string Label, DateOnly FromDate, DateOnly ToDate);
