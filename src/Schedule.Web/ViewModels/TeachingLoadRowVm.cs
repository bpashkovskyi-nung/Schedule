namespace Schedule.Web.ViewModels;

public sealed class TeachingLoadRowVm
{
    public required string TeacherName { get; init; }
    public required IReadOnlyDictionary<string, int> HoursByType { get; init; }
    public required int TotalHours { get; init; }
}
