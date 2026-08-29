using Dekanat.ScheduleSdk.Models;
using Schedule.Contracts.ReferenceData;
using Schedule.Contracts.Schedules;

namespace Schedule.Api.Services;

public interface IScheduleApiService
{
    Task<ReferenceDataResponse> GetReferenceDataAsync(CancellationToken cancellationToken = default);

    Task<PsRozkladExport> GetScheduleExportAsync(
        ScheduleTargetType targetType,
        string objectId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<ScheduleResponse> GetScheduleAsync(
        ScheduleTargetType targetType,
        string objectId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    string BuildIcalUrl(ScheduleTargetType targetType, string objectId, DateOnly from, DateOnly to);
}
