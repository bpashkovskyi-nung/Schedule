namespace Schedule.Web.ViewModels;

public static class ScheduleLookup
{
    public static string? FindRoomName(ReferenceDataVm reference, string? roomId)
    {
        if (string.IsNullOrEmpty(roomId))
        {
            return null;
        }

        foreach (BuildingVm block in reference.Blocks)
        {
            ScheduleEntityVm? room = block.Rooms.FirstOrDefault(r => r.Id == roomId);
            if (room is not null)
            {
                return room.Name;
            }
        }

        return "Невідома аудиторія";
    }

    public static string? FindTeacherName(ReferenceDataVm reference, string? teacherId)
    {
        if (string.IsNullOrEmpty(teacherId))
        {
            return null;
        }

        foreach (DepartmentVm dept in reference.TeacherDepartments)
        {
            ScheduleEntityVm? teacher = dept.Entities.FirstOrDefault(t => t.Id == teacherId);
            if (teacher is not null)
            {
                return teacher.FullName;
            }
        }

        return "Невідомий викладач";
    }

    public static string? FindGroupName(ReferenceDataVm reference, string? groupId)
    {
        if (string.IsNullOrEmpty(groupId))
        {
            return null;
        }

        foreach (DepartmentVm dept in reference.GroupDepartments)
        {
            ScheduleEntityVm? group = dept.Entities.FirstOrDefault(g => g.Id == groupId);
            if (group is not null)
            {
                return group.Name;
            }
        }

        return "Невідома група";
    }

    public static IReadOnlyList<ScheduleEntityVm> GetRoomsForBlock(ReferenceDataVm reference, string? blockName)
    {
        if (string.IsNullOrWhiteSpace(blockName))
        {
            return [];
        }

        return reference.Blocks.FirstOrDefault(b => b.Name == blockName)?.Rooms ?? [];
    }

    public static IReadOnlyList<ScheduleEntityVm> GetTeachersForDepartment(ReferenceDataVm reference, string? departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
        {
            return [];
        }

        return reference.TeacherDepartments.FirstOrDefault(d => d.Name == departmentName)?.Entities ?? [];
    }

    public static IReadOnlyList<ScheduleEntityVm> GetGroupsForDepartment(ReferenceDataVm reference, string? departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
        {
            return [];
        }

        return reference.GroupDepartments.FirstOrDefault(d => d.Name == departmentName)?.Entities ?? [];
    }
}
