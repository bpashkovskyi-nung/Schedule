using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Schedule.Contracts.AuditoriumLoad;
using Schedule.Contracts.ReferenceData;
using Schedule.Contracts.Schedules;
using Schedule.Contracts.Suggestions;
using Schedule.Contracts.Workload;

namespace Schedule.Client.Core.Api;

public sealed class ScheduleApiClient(HttpClient httpClient) : IScheduleApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ReferenceDataResponse> GetReferenceDataAsync(CancellationToken cancellationToken = default) =>
        GetAsync<ReferenceDataResponse>("api/v1/reference", cancellationToken);

    public Task<ScheduleResponse> GetScheduleAsync(
        ScheduleTargetType targetType,
        string objectId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        string target = targetType switch
        {
            ScheduleTargetType.Teacher => "teacher",
            ScheduleTargetType.Group => "group",
            ScheduleTargetType.Room => "room",
            _ => throw new ArgumentOutOfRangeException(nameof(targetType)),
        };

        string path = $"api/v1/schedules/{target}/{Uri.EscapeDataString(objectId)}" +
                      $"?from={FormatDate(from)}&to={FormatDate(to)}";
        return GetAsync<ScheduleResponse>(path, cancellationToken);
    }

    public Task<SuggestionsResponse> GetTeacherSuggestionsAsync(
        int facultyId,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        string path = $"api/v1/suggestions/teachers?facultyId={facultyId}" +
                      (string.IsNullOrWhiteSpace(query) ? string.Empty : $"&query={Uri.EscapeDataString(query)}");
        return GetAsync<SuggestionsResponse>(path, cancellationToken);
    }

    public Task<SuggestionsResponse> GetGroupSuggestionsAsync(
        int facultyId,
        int course = 0,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        string path = $"api/v1/suggestions/groups?facultyId={facultyId}&course={course}" +
                      (string.IsNullOrWhiteSpace(query) ? string.Empty : $"&query={Uri.EscapeDataString(query)}");
        return GetAsync<SuggestionsResponse>(path, cancellationToken);
    }

    public Task<TeachingLoadResponse> GetTeachingLoadAsync(
        string department,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        string path = $"api/v1/workload/teachers?department={Uri.EscapeDataString(department)}" +
                      $"&from={FormatDate(from)}&to={FormatDate(to)}";
        return GetAsync<TeachingLoadResponse>(path, cancellationToken);
    }

    public Task<AuditoriumLoadResponse> GetAuditoriumLoadAsync(
        string? building,
        DateOnly from,
        DateOnly to,
        int pairsFrom = 1,
        int pairsTo = 8,
        CancellationToken cancellationToken = default)
    {
        string path = "api/v1/workload/auditoriums" +
                      $"?from={FormatDate(from)}&to={FormatDate(to)}" +
                      $"&pairsFrom={pairsFrom}&pairsTo={pairsTo}" +
                      (string.IsNullOrWhiteSpace(building) ? string.Empty : $"&building={Uri.EscapeDataString(building)}");
        return GetAsync<AuditoriumLoadResponse>(path, cancellationToken);
    }

    public Uri GetIcalUri(ScheduleTargetType targetType, string objectId, DateOnly from, DateOnly to)
    {
        string target = targetType switch
        {
            ScheduleTargetType.Teacher => "teacher",
            ScheduleTargetType.Group => "group",
            ScheduleTargetType.Room => "room",
            _ => throw new ArgumentOutOfRangeException(nameof(targetType)),
        };

        Uri baseUri = httpClient.BaseAddress ?? throw new InvalidOperationException("API base address is not configured.");
        return new Uri(baseUri, $"api/v1/schedules/{target}/{Uri.EscapeDataString(objectId)}/ical" +
                              $"?from={FormatDate(from)}&to={FormatDate(to)}");
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(path, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            T? result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return result ?? throw new ScheduleApiException("API повернув порожню відповідь.");
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        string message = TryGetErrorMessage(body) ?? $"API повернув HTTP {(int)response.StatusCode}.";
        throw new ScheduleApiException(message, (int)response.StatusCode, TryGetErrorCode(body));
    }

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd");

    private static string? TryGetErrorMessage(string body)
    {
        try
        {
            using JsonDocument json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("message", out JsonElement message)) return message.GetString();
            if (json.RootElement.TryGetProperty("title", out JsonElement title)) return title.GetString();
        }
        catch (JsonException)
        {
            // The server may return a non-JSON error body.
        }

        return null;
    }

    private static string? TryGetErrorCode(string body)
    {
        try
        {
            using JsonDocument json = JsonDocument.Parse(body);
            return json.RootElement.TryGetProperty("code", out JsonElement code) ? code.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
