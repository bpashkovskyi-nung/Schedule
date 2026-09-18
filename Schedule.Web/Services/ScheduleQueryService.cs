using Schedule.Client.Core.Api;
using Schedule.Contracts.AuditoriumLoad;
using Schedule.Contracts.Schedules;
using Schedule.Contracts.Suggestions;
using Schedule.Contracts.Workload;

namespace Schedule.Web.Services;

public sealed class ScheduleQueryService(IScheduleApiClient apiClient) : IScheduleQueryService
{
    public Task<ScheduleResponse> GetScheduleAsync(
        ScheduleTargetType targetType,
        string objectId,
        DateOnly beginDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default) =>
        apiClient.GetScheduleAsync(targetType, objectId, beginDate, endDate, cancellationToken);

    public Task<SuggestionsResponse> GetTeacherSuggestionsAsync(
        int facultyId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetTeacherSuggestionsAsync(facultyId, cancellationToken: cancellationToken);

    public Task<SuggestionsResponse> GetGroupSuggestionsAsync(
        int facultyId,
        int course,
        CancellationToken cancellationToken = default) =>
        apiClient.GetGroupSuggestionsAsync(facultyId, course, cancellationToken: cancellationToken);

    public Task<TeachingLoadResponse> GetTeachingLoadAsync(
        string department,
        DateOnly beginDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default) =>
        apiClient.GetTeachingLoadAsync(department, beginDate, endDate, cancellationToken);

    public Task<AuditoriumLoadResponse> GetAuditoriumLoadAsync(
        string? building,
        DateOnly beginDate,
        DateOnly endDate,
        int pairsFrom,
        int pairsTo,
        CancellationToken cancellationToken = default) =>
        apiClient.GetAuditoriumLoadAsync(building, beginDate, endDate, pairsFrom, pairsTo, cancellationToken);

    public string BuildExportUrl(
        ScheduleTargetType targetType,
        string objectId,
        DateOnly beginDate,
        DateOnly endDate) =>
        apiClient.GetIcalUri(targetType, objectId, beginDate, endDate).ToString();
}
