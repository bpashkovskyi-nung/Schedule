namespace Schedule.Contracts.AuditoriumLoad;

public sealed record AuditoriumLoadItemDto
{
    public required string RoomName { get; init; }
    public required string BuildingName { get; init; }
    public required double Percent { get; init; }
    public required int ActualPairs { get; init; }
    public required int PossiblePairs { get; init; }
    public IReadOnlyList<AuditoriumLoadLessonDto> Lessons { get; init; } = [];
}
