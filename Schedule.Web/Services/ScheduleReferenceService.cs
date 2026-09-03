using Dekanat.ScheduleSdk.Enums;
using Dekanat.ScheduleSdk.Models;
using Schedule.Web.ViewModels;

namespace Schedule.Web.Services;

public sealed class ScheduleReferenceService(IScheduleRepository repository) : IScheduleReferenceService
{
    public async Task<ReferenceDataVm> LoadReferenceDataAsync(CancellationToken cancellationToken = default)
    {
        Task<PsRozkladExport> roomsTask = repository.GetObjectListAsync(RequestMode.Room, cancellationToken);
        Task<PsRozkladExport> teachersTask = repository.GetObjectListAsync(RequestMode.Teacher, cancellationToken);
        Task<PsRozkladExport> groupsTask = repository.GetObjectListAsync(RequestMode.Group, cancellationToken);

        await Task.WhenAll(roomsTask, teachersTask, groupsTask);

        return new ReferenceDataVm
        {
            Blocks = MapBuildings(await roomsTask),
            TeacherDepartments = MapDepartments(await teachersTask),
            GroupDepartments = MapDepartments(await groupsTask),
        };
    }

    private static IReadOnlyList<BuildingVm> MapBuildings(PsRozkladExport export) =>
        (export.Blocks ?? [])
            .Where(b => b.Objects is { Count: > 0 })
            .Select(b => new BuildingVm
            {
                Name = b.Name ?? string.Empty,
                Rooms = MapEntities(b.Objects),
            })
            .ToList();

    private static IReadOnlyList<DepartmentVm> MapDepartments(PsRozkladExport export) =>
        (export.Departments ?? [])
            .Where(d => d.Objects is { Count: > 0 })
            .Select(d => new DepartmentVm
            {
                Name = d.Name ?? string.Empty,
                Entities = MapEntities(d.Objects),
            })
            .ToList();

    private static IReadOnlyList<ScheduleEntityVm> MapEntities(IReadOnlyList<ScheduleEntity>? entities) =>
        (entities ?? [])
            .Select(e => new ScheduleEntityVm
            {
                Id = e.Id ?? string.Empty,
                Name = e.Name ?? string.Empty,
                LastName = e.LastName,
                FirstName = e.FirstName,
                Patronymic = e.Patronymic,
                FullName = BuildTeacherFullName(e),
            })
            .ToList();

    internal static string BuildTeacherFullName(ScheduleEntity entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.LastName))
        {
            return $"{entity.LastName} {entity.FirstName} {entity.Patronymic}".Replace("  ", " ").Trim();
        }

        return entity.Name ?? string.Empty;
    }
}
