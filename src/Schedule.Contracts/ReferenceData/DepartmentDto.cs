namespace Schedule.Contracts.ReferenceData;

public sealed record DepartmentDto
{
    public required string Name { get; init; }
    public required IReadOnlyList<ScheduleEntityDto> Entities { get; init; }
}
