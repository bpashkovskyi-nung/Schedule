namespace Schedule.Web.ViewModels;

public sealed class DayScheduleVm
{
    public required string Date { get; init; }
    public required string DayName { get; init; }
    public required IReadOnlyList<ScheduleItemVm> Lessons { get; init; }
}
