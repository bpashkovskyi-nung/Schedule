namespace Schedule.Api.Configuration;

public sealed class ScheduleApiOptions
{
    public const string SectionName = "ScheduleApi";

    public int MaxLoadDays { get; set; } = 31;
    public int AuditoriumConcurrency { get; set; } = 4;
    public int ReferenceCacheMinutes { get; set; } = 10;
    public int ExternalRequestTimeoutSeconds { get; set; } = 60;
    public int ExternalRetryCount { get; set; } = 2;
    public int WorkloadPermitLimit { get; set; } = 2;
    public int WorkloadWindowSeconds { get; set; } = 30;
    public bool EnableOpenApi { get; set; }
    public string TimetableBaseUrl { get; set; } = "https://dekanat.nung.edu.ua/cgi-bin/timetable.cgi";
}
