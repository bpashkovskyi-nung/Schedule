namespace Schedule.Web.ViewModels;

public sealed class TeachingLoadFormVm : DateRangeFormVm
{
    public string? DepartmentName { get; set; }
    public TeachingLoadResultVm? Result { get; set; }
}
