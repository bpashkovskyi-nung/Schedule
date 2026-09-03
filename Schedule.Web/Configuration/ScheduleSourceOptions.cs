namespace Schedule.Web.Configuration;

public sealed class ScheduleSourceOptions
{
    public const string SectionName = "ScheduleSource";

    public string Provider { get; set; } = "Dekanat";

    public string ConnectionStringName { get; set; } = "ScheduleDatabase";

    public bool IsDatabase => string.Equals(Provider, "Database", StringComparison.OrdinalIgnoreCase);
}
