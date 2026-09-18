namespace Schedule.Web.ViewModels;

public sealed class ScheduleIndexViewModel
{
    public string ActiveTab { get; set; } = "teachers";
    public string? ErrorMessage { get; set; }
    public required IReadOnlyList<PeriodOptionVm> PeriodOptions { get; init; }
    public required ReferenceDataVm Reference { get; init; }
    public RoomsScheduleFormVm Rooms { get; set; } = new();
    public TeachersScheduleFormVm Teachers { get; set; } = new();
    public GroupsScheduleFormVm Groups { get; set; } = new();
    public TeachingLoadFormVm TeachingLoad { get; set; } = new();
    public AuditoriumLoadFormVm AuditoriumLoad { get; set; } = new();

}
