using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Schedule.Api.Configuration;

namespace Schedule.Api.Services;

public interface ITimetableSuggestionsService
{
    Task<IReadOnlyList<string>> GetSuggestionsAsync(
        int level,
        int facultyId,
        int course = 0,
        string? query = null,
        CancellationToken cancellationToken = default);
}

public sealed class TimetableSuggestionsService(
    HttpClient httpClient,
    IOptions<ScheduleApiOptions> options) : ITimetableSuggestionsService
{
    private readonly ScheduleApiOptions apiOptions = options.Value;

    public async Task<IReadOnlyList<string>> GetSuggestionsAsync(
        int level,
        int facultyId,
        int course = 0,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        if (level is not (141 or 142)) throw new ArgumentOutOfRangeException(nameof(level));
        if (facultyId <= 0) throw new ArgumentOutOfRangeException(nameof(facultyId));

        string url = $"{apiOptions.TimetableBaseUrl}?n=701&lev={level}&faculty={facultyId}";
        if (course > 0) url += $"&course={course}";
        if (!string.IsNullOrWhiteSpace(query)) url += $"&query={Uri.EscapeDataString(query)}";

        using JsonDocument document = await httpClient.GetFromJsonAsync<JsonDocument>(url, cancellationToken)
            ?? throw new InvalidOperationException("Suggestions API returned an empty response.");

        if (!document.RootElement.TryGetProperty("suggestions", out JsonElement suggestions) ||
            suggestions.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return suggestions.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString() ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }
}
