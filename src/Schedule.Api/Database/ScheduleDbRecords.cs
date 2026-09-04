namespace Schedule.Api.Database;

internal sealed class ScheduleDepartmentRecord
{
    public int Id { get; set; }

    public string TargetType { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<ScheduleEntityRecord> Entities { get; set; } = [];
}

internal sealed class ScheduleEntityRecord
{
    public long Id { get; set; }

    public string TargetType { get; set; } = string.Empty;

    public string ExternalId { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? Patronymic { get; set; }

    public int SortOrder { get; set; }
}

internal sealed class ScheduleBuildingRecord
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

internal sealed class ScheduleRoomRecord
{
    public long Id { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int BuildingId { get; set; }

    public int SortOrder { get; set; }
}

internal sealed class ScheduleLessonRecord
{
    public long Id { get; set; }

    public string TargetType { get; set; } = string.Empty;

    public string ObjectId { get; set; } = string.Empty;

    public string? ObjectName { get; set; }

    public DateOnly Date { get; set; }

    public string? Comment { get; set; }

    public string? LessonNumber { get; set; }

    public string? LessonName { get; set; }

    public string? LessonTime { get; set; }

    public string? LessonDescription { get; set; }

    public string? Half { get; set; }

    public string? Teacher { get; set; }

    public string? AdditionalTeachers { get; set; }

    public string? Room { get; set; }

    public string? Group { get; set; }

    public string? Title { get; set; }

    public string? Type { get; set; }

    public string? Replacement { get; set; }

    public string? Reservation { get; set; }

    public string? Online { get; set; }

    public string? LinkComment { get; set; }

    public string? Link { get; set; }

    public int SortOrder { get; set; }
}
