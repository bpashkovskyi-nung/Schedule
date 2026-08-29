namespace Schedule.Contracts.Suggestions;

public sealed record SuggestionsResponse
{
    public required IReadOnlyList<string> Suggestions { get; init; }
}
