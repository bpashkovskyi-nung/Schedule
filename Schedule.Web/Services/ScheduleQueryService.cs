using Dekanat.ScheduleSdk.Enums;
using Dekanat.ScheduleSdk.Models;
using Dekanat.ScheduleSdk.Options;
using Microsoft.Extensions.Options;
using Schedule.Web.Configuration;

namespace Schedule.Web.Services;

public sealed class ScheduleQueryService(
    IScheduleRepository repository,
    IOptions<LegacyFeatureOptions> featureOptions) : IScheduleQueryService
{
    public Task<PsRozkladExport> GetScheduleAsync(
        RequestMode mode,
        string objectId,
        DateOnly beginDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default) =>
        repository.GetScheduleAsync(mode, objectId, beginDate, endDate, cancellationToken);

    public string BuildExportUrl(RequestMode mode, string objectId, DateOnly beginDate, DateOnly endDate)
    {
        string begin = beginDate.ToString("dd.MM.yyyy");
        string end = endDate.ToString("dd.MM.yyyy");
        string modeValue = mode switch
        {
            RequestMode.Teacher => "teacher",
            RequestMode.Group => "group",
            RequestMode.Room => "room",
            _ => "group",
        };

        if (featureOptions.Value.GenerateIcalFromJson || repository?.SupportsDirectIcalExport == false)
        {
            return $"/Home/Ical?mode={modeValue}&objectId={Uri.EscapeDataString(objectId)}" +
                   $"&from={beginDate:yyyy-MM-dd}&to={endDate:yyyy-MM-dd}";
        }

        return $"{PsRozkladClientOptions.DefaultBaseUrl}?req_type=rozklad&req_mode={modeValue}" +
               $"&OBJ_ID={Uri.EscapeDataString(objectId)}&OBJ_name=&dep_name=&ros_text=united" +
               $"&begin_date={begin}&end_date={end}&req_format=iCal&coding_mode=UTF8&bs=ok";
    }
}
