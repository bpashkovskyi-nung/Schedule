using Schedule.Contracts.Common;

namespace Schedule.Contracts.AuditoriumLoad;

public sealed record AuditoriumLoadResponse
{
    public required IReadOnlyList<AuditoriumLoadItemDto> Items { get; init; }
    public required int ErrorCount { get; init; }
    public required int WorkingDays { get; init; }
    public required int PossiblePairsPerRoom { get; init; }
    public required DateRangeDto Period { get; init; }
    public required int PairsFrom { get; init; }
    public required int PairsTo { get; init; }
}
