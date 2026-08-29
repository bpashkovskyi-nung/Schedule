namespace Schedule.Contracts.ReferenceData;

public sealed record BuildingDto
{
    public required string Name { get; init; }
    public required IReadOnlyList<ScheduleEntityDto> Rooms { get; init; }
}
