using Dekanat.ScheduleSdk.Enums;
using Dekanat.ScheduleSdk.Models;

namespace Schedule.Api.Services;

public interface IScheduleDataSource
{
    bool IsDatabase { get; }

    Task<PsRozkladExport> GetObjectListAsync(
        RequestMode mode,
        CancellationToken cancellationToken = default);

    Task<PsRozkladExport> GetScheduleAsync(
        RequestMode mode,
        string objectId,
        DateOnly beginDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
