namespace Schedule.Web.ViewModels;

public class DateRangeFormVm
{
    public string PeriodKey { get; set; } = "to_end_of_week";
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public bool HidePastDates { get; set; } = true;
}
