namespace Schedule.Web.ViewModels;

public sealed class TeachersScheduleFormVm : DateRangeFormVm
{
    public string? DepartmentName { get; set; }
    public string? TeacherId { get; set; }
    public ScheduleResultVm? Result { get; set; }
}
