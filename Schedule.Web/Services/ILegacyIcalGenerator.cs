using Dekanat.ScheduleSdk.Models;

namespace Schedule.Web.Services;

public interface ILegacyIcalGenerator
{
    byte[] Generate(PsRozkladExport export, string calendarName);
}
