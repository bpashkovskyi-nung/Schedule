namespace Schedule.Web.ViewModels;

public sealed class RoomsScheduleFormVm : DateRangeFormVm
{
    public string? BlockName { get; set; }
    public string? RoomId { get; set; }
    public ScheduleResultVm? Result { get; set; }
}
