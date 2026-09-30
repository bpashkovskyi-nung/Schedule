namespace Schedule.Web.ViewModels;

public sealed class ReferenceDataVm
{
    public required IReadOnlyList<BuildingVm> Blocks { get; init; }
    public required IReadOnlyList<DepartmentVm> TeacherDepartments { get; init; }
    public required IReadOnlyList<DepartmentVm> GroupDepartments { get; init; }
}
