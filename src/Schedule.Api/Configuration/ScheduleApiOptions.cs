namespace Schedule.Api.Configuration;

public sealed class ScheduleApiOptions
{
    public const string SectionName = "ScheduleApi";

    public int MaxScheduleDays { get; set; } = 93;
    public int MaxLoadDays { get; set; } = 31;
    public int AuditoriumConcurrency { get; set; } = 4;
    public int ReferenceCacheMinutes { get; set; } = 10;
    public string TimetableBaseUrl { get; set; } = "https://dekanat.nung.edu.ua/cgi-bin/timetable.cgi";
}
