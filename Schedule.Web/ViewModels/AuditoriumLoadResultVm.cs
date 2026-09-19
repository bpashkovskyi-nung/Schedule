namespace Schedule.Web.ViewModels;

public sealed class AuditoriumLoadResultVm
{
    public required IReadOnlyList<AuditoriumLoadItemVm> Items { get; init; }
    public int ErrorCount { get; init; }
    public int WorkingDays { get; init; }
    public int PossiblePairsPerRoom { get; init; }
    public required IReadOnlyList<DateOnly> Dates { get; init; }
}
