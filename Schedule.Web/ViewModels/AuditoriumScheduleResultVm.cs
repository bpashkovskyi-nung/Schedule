namespace Schedule.Web.ViewModels;

public sealed class AuditoriumScheduleResultVm
{
    public required DateOnly Date { get; init; }
    public required IReadOnlyList<AuditoriumLoadItemVm> Items { get; init; }
}
