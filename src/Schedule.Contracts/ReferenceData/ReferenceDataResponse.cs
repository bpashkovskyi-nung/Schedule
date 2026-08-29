namespace Schedule.Contracts.ReferenceData;

public sealed record ReferenceDataResponse
{
    public required IReadOnlyList<BuildingDto> Buildings { get; init; }
    public required IReadOnlyList<DepartmentDto> TeacherDepartments { get; init; }
    public required IReadOnlyList<DepartmentDto> GroupDepartments { get; init; }
}
