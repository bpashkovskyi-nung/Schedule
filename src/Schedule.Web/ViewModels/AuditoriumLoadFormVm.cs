namespace Schedule.Web.ViewModels;

public sealed class AuditoriumLoadFormVm : DateRangeFormVm
{
    public string? BlockName { get; set; }
    public int PairsFrom { get; set; } = 1;
    public int PairsTo { get; set; } = 8;
    public AuditoriumLoadResultVm? Result { get; set; }
    public string? Summary { get; set; }
}
