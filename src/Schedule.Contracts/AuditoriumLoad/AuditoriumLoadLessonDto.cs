namespace Schedule.Contracts.AuditoriumLoad;

public sealed record AuditoriumLoadLessonDto
{
    public required DateOnly Date { get; init; }
    public required int PairNumber { get; init; }
    public required string PairTime { get; init; }
    public required string Description { get; init; }
}
