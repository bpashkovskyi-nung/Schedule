namespace Schedule.Web.ViewModels;

public sealed class BuildingVm
{
    public required string Name { get; init; }
    public required IReadOnlyList<ScheduleEntityVm> Rooms { get; init; }
}
