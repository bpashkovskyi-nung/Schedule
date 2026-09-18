namespace Schedule.Contracts.AuditoriumLoad;

public sealed record AuditoriumLoadResponse
{
    public required IReadOnlyList<AuditoriumLoadItemDto> Items { get; init; }
    public required int ErrorCount { get; init; }
    public required int WorkingDays { get; init; }
    public required int PossiblePairsPerRoom { get; init; }
}
