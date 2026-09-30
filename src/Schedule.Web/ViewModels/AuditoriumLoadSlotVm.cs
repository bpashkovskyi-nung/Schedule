namespace Schedule.Web.ViewModels;

public sealed class AuditoriumLoadSlotVm
{
    public required DateOnly Date { get; init; }
    public required int PairNumber { get; init; }
    public required string PairTime { get; init; }
    public required string Description { get; init; }

    public string Tooltip =>
        $"{Date:dd.MM.yyyy} · пара {PairNumber} · {PairTime}\n{Description}";
}
