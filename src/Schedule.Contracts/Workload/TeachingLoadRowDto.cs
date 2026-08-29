namespace Schedule.Contracts.Workload;

public sealed record TeachingLoadRowDto
{
    public required string TeacherName { get; init; }
    public required IReadOnlyDictionary<string, int> HoursByType { get; init; }
    public required int TotalHours { get; init; }
}
