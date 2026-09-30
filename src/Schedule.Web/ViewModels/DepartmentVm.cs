namespace Schedule.Web.ViewModels;

public sealed class DepartmentVm
{
    public required string Name { get; init; }
    public required IReadOnlyList<ScheduleEntityVm> Entities { get; init; }
}
