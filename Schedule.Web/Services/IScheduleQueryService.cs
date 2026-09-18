using Schedule.Contracts.AuditoriumLoad;
using Schedule.Contracts.Schedules;
using Schedule.Contracts.Suggestions;
using Schedule.Contracts.Workload;

namespace Schedule.Web.Services;

public interface IScheduleQueryService
{
    Task<ScheduleResponse> GetScheduleAsync(
        ScheduleTargetType targetType,
        string objectId,
        DateOnly beginDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<SuggestionsResponse> GetTeacherSuggestionsAsync(
        int facultyId,
        CancellationToken cancellationToken = default);

    Task<SuggestionsResponse> GetGroupSuggestionsAsync(
        int facultyId,
        int course,
        CancellationToken cancellationToken = default);

    Task<TeachingLoadResponse> GetTeachingLoadAsync(
        string department,
        DateOnly beginDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<AuditoriumLoadResponse> GetAuditoriumLoadAsync(
        string? building,
        DateOnly beginDate,
        DateOnly endDate,
        int pairsFrom,
        int pairsTo,
        CancellationToken cancellationToken = default);

    string BuildExportUrl(
        ScheduleTargetType targetType,
        string objectId,
        DateOnly beginDate,
        DateOnly endDate);
}
