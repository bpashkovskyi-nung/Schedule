namespace Schedule.Web.ViewModels;

public sealed class ScheduleItemVm
{
    public required string LessonNumber { get; init; }
    public required string LessonTime { get; init; }
    public required string LessonDescription { get; init; }
    public IReadOnlyList<ScheduleLinkVm> Links { get; init; } = [];
}
