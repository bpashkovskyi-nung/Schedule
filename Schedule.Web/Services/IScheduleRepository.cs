using Dekanat.ScheduleSdk.Enums;
using Dekanat.ScheduleSdk.Models;

namespace Schedule.Web.Services;

public interface IScheduleRepository
{
    bool SupportsDirectIcalExport { get; }

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
