namespace Schedule.Contracts.Workload;

public sealed record TeachingLoadResponse
{
    public required IReadOnlyList<string> SessionTypes { get; init; }
    public required IReadOnlyList<TeachingLoadRowDto> Rows { get; init; }
}
