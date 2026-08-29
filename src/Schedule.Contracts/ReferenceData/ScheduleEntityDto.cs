namespace Schedule.Contracts.ReferenceData;

public sealed record ScheduleEntityDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? LastName { get; init; }
    public string? FirstName { get; init; }
    public string? Patronymic { get; init; }
    public required string FullName { get; init; }
}
