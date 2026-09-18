namespace Schedule.Contracts.ReferenceData;

public sealed record ScheduleEntityDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string FullName { get; init; }
}
