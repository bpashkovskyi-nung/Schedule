namespace Schedule.Web.Database;

internal sealed class ScheduleEntityRecord
{
    public long Id { get; set; }

    public string TargetType { get; set; } = string.Empty;

    public string ExternalId { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? Patronymic { get; set; }

    public int SortOrder { get; set; }

    public ScheduleDepartmentRecord? Department { get; set; }
}
