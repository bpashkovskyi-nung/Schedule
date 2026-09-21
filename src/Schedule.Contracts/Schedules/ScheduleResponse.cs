using Schedule.Contracts.Common;

namespace Schedule.Contracts.Schedules;

public sealed record ScheduleResponse
{
    public required ScheduleTargetType TargetType { get; init; }
    public required string ObjectId { get; init; }
    public string ObjectName { get; init; } = string.Empty;
    public required DateRangeDto Period { get; init; }
    public required IReadOnlyList<LessonDto> Lessons { get; init; }
}
