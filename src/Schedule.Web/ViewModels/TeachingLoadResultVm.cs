namespace Schedule.Web.ViewModels;

public sealed class TeachingLoadResultVm
{
    public required IReadOnlyList<string> SessionTypes { get; init; }
    public required IReadOnlyList<TeachingLoadRowVm> Rows { get; init; }
}
