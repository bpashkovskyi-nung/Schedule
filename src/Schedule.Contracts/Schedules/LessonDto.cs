namespace Schedule.Contracts.Schedules;

public sealed record LessonDto
{
    public DateOnly? Date { get; init; }
    public string LessonNumber { get; init; } = string.Empty;
    public string LessonName { get; init; } = string.Empty;
    public string LessonTime { get; init; } = string.Empty;
    public string LessonDescription { get; init; } = string.Empty;
    public string Teacher { get; init; } = string.Empty;
    public string Room { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Replacement { get; init; } = string.Empty;
    public string Online { get; init; } = string.Empty;
    public string LinkComment { get; init; } = string.Empty;
    public string Link { get; init; } = string.Empty;
}
