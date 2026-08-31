using global::Schedule.Contracts.Schedules;

namespace Schedule.Mobile.Services;

public sealed class MobileScheduleSelection
{
    public ScheduleTargetType TargetType { get; set; } = ScheduleTargetType.Group;
    public string ObjectId { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(6));
    public bool HidePastDates { get; set; }

    public bool HasSelection => !string.IsNullOrWhiteSpace(ObjectId);
}
