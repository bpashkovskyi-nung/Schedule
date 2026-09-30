namespace Schedule.Web.ViewModels;

public sealed class GroupsScheduleFormVm : DateRangeFormVm
{
    public string? DepartmentName { get; set; }
    public string? GroupId { get; set; }
    public ScheduleResultVm? Result { get; set; }
}
