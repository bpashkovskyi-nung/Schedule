using Schedule.Contracts.AuditoriumLoad;
using Schedule.Contracts.ReferenceData;
using Schedule.Contracts.Schedules;
using Schedule.Contracts.Suggestions;
using Schedule.Contracts.Workload;

namespace Schedule.Client.Core.Api;

public interface IScheduleApiClient
{
    Task<ReferenceDataResponse> GetReferenceDataAsync(CancellationToken cancellationToken = default);

    Task<ScheduleResponse> GetScheduleAsync(
        ScheduleTargetType targetType,
        string objectId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<SuggestionsResponse> GetTeacherSuggestionsAsync(
        int facultyId,
        string? query = null,
        CancellationToken cancellationToken = default);

    Task<SuggestionsResponse> GetGroupSuggestionsAsync(
        int facultyId,
        int course = 0,
        string? query = null,
        CancellationToken cancellationToken = default);

    Task<TeachingLoadResponse> GetTeachingLoadAsync(
        string department,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<AuditoriumLoadResponse> GetAuditoriumLoadAsync(
        string? building,
        DateOnly from,
        DateOnly to,
        int pairsFrom = 1,
        int pairsTo = 8,
        CancellationToken cancellationToken = default);

    Uri GetIcalUri(ScheduleTargetType targetType, string objectId, DateOnly from, DateOnly to);
}
