namespace Schedule.Web.ViewModels;

public sealed class PeriodOptionVm(string key, string label, DateTime from, DateTime to)
{
    public string Key { get; } = key;
    public string Label { get; } = label;
    public DateOnly FromDate { get; } = DateOnly.FromDateTime(from);
    public DateOnly ToDate { get; } = DateOnly.FromDateTime(to);
}
