using Microsoft.AspNetCore.Mvc;
using Schedule.Api.Services;
using Schedule.Contracts.Suggestions;

namespace Schedule.Api.Controllers;

[Route("api/v1/suggestions")]
public sealed class SuggestionsController(ITimetableSuggestionsService suggestionsService) : ApiControllerBase
{
    [HttpGet("teachers")]
    public async Task<SuggestionsResponse> GetTeachers(
        [FromQuery] int facultyId,
        [FromQuery] string? query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> suggestions = await suggestionsService.GetSuggestionsAsync(
            141, facultyId, query: query, cancellationToken: cancellationToken);
        return new SuggestionsResponse { Suggestions = suggestions };
    }

    [HttpGet("groups")]
    public async Task<SuggestionsResponse> GetGroups(
        [FromQuery] int facultyId,
        [FromQuery] int course,
        [FromQuery] string? query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> suggestions = await suggestionsService.GetSuggestionsAsync(
            142, facultyId, course, query, cancellationToken);
        return new SuggestionsResponse { Suggestions = suggestions };
    }
}
