namespace Schedule.Web.ViewModels;

public sealed class ScheduleIndexViewModel
{
    public string ActiveTab { get; set; } = "teachers";
    public string? ErrorMessage { get; set; }
    public required IReadOnlyList<PeriodOptionVm> PeriodOptions { get; init; }
    public required ReferenceDataVm Reference { get; init; }
    public UnifiedScheduleFormVm Unified { get; set; } = new();
    public RoomsScheduleFormVm Rooms { get; set; } = new();
    public TeachersScheduleFormVm Teachers { get; set; } = new();
    public GroupsScheduleFormVm Groups { get; set; } = new();
    public TeachingLoadFormVm TeachingLoad { get; set; } = new();
    public AuditoriumLoadFormVm AuditoriumLoad { get; set; } = new();

    public IEnumerable<(int Value, string Label)> CourseOptions =>
    [
        (0, "Усі курси"),
        (1, "1"),
        (2, "2"),
        (3, "3"),
        (4, "4"),
        (5, "5"),
        (6, "6"),
    ];
}
