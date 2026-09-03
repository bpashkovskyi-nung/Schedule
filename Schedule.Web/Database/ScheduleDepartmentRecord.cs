namespace Schedule.Web.Database;

internal sealed class ScheduleDepartmentRecord
{
    public int Id { get; set; }

    public string TargetType { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<ScheduleEntityRecord> Entities { get; set; } = [];
}
