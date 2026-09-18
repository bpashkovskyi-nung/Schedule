using Schedule.Client.Core.Api;
using Schedule.Contracts.ReferenceData;
using Schedule.Web.ViewModels;

namespace Schedule.Web.Services;

public sealed class ScheduleReferenceService(IScheduleApiClient apiClient) : IScheduleReferenceService
{
    public async Task<ReferenceDataVm> LoadReferenceDataAsync(CancellationToken cancellationToken = default)
    {
        ReferenceDataResponse reference = await apiClient.GetReferenceDataAsync(cancellationToken);

        return new ReferenceDataVm
        {
            Blocks = MapBuildings(reference.Buildings),
            TeacherDepartments = MapDepartments(reference.TeacherDepartments),
            GroupDepartments = MapDepartments(reference.GroupDepartments),
        };
    }

    private static IReadOnlyList<BuildingVm> MapBuildings(IReadOnlyList<BuildingDto> buildings) =>
        buildings
            .Where(building => building.Rooms.Count > 0)
            .Select(building => new BuildingVm
            {
                Name = building.Name,
                Rooms = MapEntities(building.Rooms),
            })
            .OrderBy(building => building.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static IReadOnlyList<DepartmentVm> MapDepartments(IReadOnlyList<DepartmentDto> departments) =>
        departments
            .Where(department => department.Entities.Count > 0)
            .Select(department => new DepartmentVm
            {
                Name = department.Name,
                Entities = MapEntities(department.Entities),
            })
            .OrderBy(department => department.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static IReadOnlyList<ScheduleEntityVm> MapEntities(IReadOnlyList<ScheduleEntityDto> entities) =>
        entities
            .Select(entity => new ScheduleEntityVm
            {
                Id = entity.Id,
                Name = entity.Name,
                LastName = entity.LastName,
                FirstName = entity.FirstName,
                Patronymic = entity.Patronymic,
                FullName = entity.FullName,
            })
            .OrderBy(entity => entity.FullName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entity => entity.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
}
