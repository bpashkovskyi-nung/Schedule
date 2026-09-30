namespace Schedule.Api.Configuration;

public sealed class ScheduleSyncOptions
{
    public const string SectionName = "ScheduleSync";

    public bool Enabled { get; set; }

    public bool RunOnStartup { get; set; } = true;

    public bool RunOnce { get; set; }

    public int IntervalHours { get; set; } = 6;

    public int RequestConcurrency { get; set; } = 4;

    public string? FromDate { get; set; }

    public string? ToDate { get; set; }

    public int DaysBeforeToday { get; set; } = 7;

    public int DaysAfterToday { get; set; } = 180;

    public (DateOnly From, DateOnly To) ResolvePeriod(DateOnly today)
    {
        DateOnly from = ParseOrDefault(FromDate, today.AddDays(-Math.Max(0, DaysBeforeToday)));
        DateOnly to = ParseOrDefault(ToDate, today.AddDays(Math.Max(0, DaysAfterToday)));

        if (to < from)
        {
            throw new InvalidOperationException(
                $"ScheduleSync period is invalid: FromDate '{from:yyyy-MM-dd}' is after ToDate '{to:yyyy-MM-dd}'.");
        }

        return (from, to);
    }

    private static DateOnly ParseOrDefault(string? value, DateOnly defaultValue)
    {
        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : DateOnly.ParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }
}
