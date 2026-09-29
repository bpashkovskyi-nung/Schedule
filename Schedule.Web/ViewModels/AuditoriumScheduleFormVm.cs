namespace Schedule.Web.ViewModels;

public sealed class AuditoriumScheduleFormVm
{
    public string? BlockName { get; set; }
    public List<string> RoomIds { get; set; } = [];
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public AuditoriumScheduleResultVm? Result { get; set; }
}
