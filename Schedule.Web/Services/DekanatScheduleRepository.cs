using Dekanat.ScheduleSdk;
using Dekanat.ScheduleSdk.Enums;
using Dekanat.ScheduleSdk.Models;
using Dekanat.ScheduleSdk.Requests;

namespace Schedule.Web.Services;

public sealed class DekanatScheduleRepository(IPsRozkladClient client) : IScheduleRepository
{
    public bool SupportsDirectIcalExport => true;

    public Task<PsRozkladExport> GetObjectListAsync(
        RequestMode mode,
        CancellationToken cancellationToken = default) =>
        client.GetObjectListAsync(new ObjectListRequest
        {
            Mode = mode,
            IncludeIds = true,
        }, cancellationToken);

    public Task<PsRozkladExport> GetScheduleAsync(
        RequestMode mode,
        string objectId,
        DateOnly beginDate,
        DateOnly endDate,
    CancellationToken cancellationToken = default)
    {
        Task<PsRozkladExport> request = client.GetScheduleAsync(new ScheduleRequest
        {
            Mode = mode,
            ObjectId = objectId,
            BeginDate = beginDate,
            EndDate = endDate,
            TextFormat = ScheduleTextFormat.Separated,
            IncludeAllStreamComponents = true,
        }, cancellationToken);

        return NormalizeScheduleAsync(request);
    }

    private static async Task<PsRozkladExport> NormalizeScheduleAsync(Task<PsRozkladExport> request)
    {
        PsRozkladExport export = await request;
        ScheduleItemDescription.Normalize(export.ScheduleItems);
        return export;
    }
}
