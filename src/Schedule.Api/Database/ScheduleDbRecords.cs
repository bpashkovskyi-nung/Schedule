namespace Schedule.Api.Database;

internal sealed class ScheduleDepartmentRecord
{
    public int Id { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

internal sealed class ScheduleGroupRecord
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ICollection<ScheduleLessonGroupRecord> LessonLinks { get; set; } = [];
}

internal sealed class ScheduleTeacherRecord
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? Patronymic { get; set; }
    public int SortOrder { get; set; }
    public ICollection<ScheduleLessonTeacherRecord> LessonLinks { get; set; } = [];
}

internal sealed class ScheduleBuildingRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ICollection<ScheduleRoomRecord> Rooms { get; set; } = [];
}

internal sealed class ScheduleRoomRecord
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int BuildingId { get; set; }
    public int SortOrder { get; set; }
    public ScheduleBuildingRecord Building { get; set; } = null!;
    public ICollection<ScheduleLessonRoomRecord> LessonLinks { get; set; } = [];
}

internal sealed class ScheduleLessonRecord
{
    public long Id { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public string? SourceHash { get; set; }
    public DateOnly Date { get; set; }
    public string? Comment { get; set; }
    public string? LessonNumber { get; set; }
    public string? LessonName { get; set; }
    public string? LessonTime { get; set; }
    public string? LessonDescription { get; set; }
    public string? Half { get; set; }
    public string? Title { get; set; }
    public string? Type { get; set; }
    public string? Replacement { get; set; }
    public string? Reservation { get; set; }
    public string? Online { get; set; }
    public string? LinkComment { get; set; }
    public string? Link { get; set; }
    public int SortOrder { get; set; }
    public long? LastSyncRunId { get; set; }
    public ICollection<ScheduleLessonGroupRecord> Groups { get; set; } = [];
    public ICollection<ScheduleLessonTeacherRecord> Teachers { get; set; } = [];
    public ICollection<ScheduleLessonRoomRecord> Rooms { get; set; } = [];
}

internal sealed class ScheduleLessonGroupRecord
{
    public long LessonId { get; set; }
    public long GroupId { get; set; }
    public string Subgroup { get; set; } = string.Empty;
    public ScheduleLessonRecord Lesson { get; set; } = null!;
    public ScheduleGroupRecord Group { get; set; } = null!;
}

internal sealed class ScheduleLessonTeacherRecord
{
    public long LessonId { get; set; }
    public long TeacherId { get; set; }
    public bool IsPrimary { get; set; }
    public ScheduleLessonRecord Lesson { get; set; } = null!;
    public ScheduleTeacherRecord Teacher { get; set; } = null!;
}

internal sealed class ScheduleLessonRoomRecord
{
    public long LessonId { get; set; }
    public long RoomId { get; set; }
    public bool IsPrimary { get; set; }
    public ScheduleLessonRecord Lesson { get; set; } = null!;
    public ScheduleRoomRecord Room { get; set; } = null!;
}

internal sealed class ScheduleSyncRunRecord
{
    public long Id { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
}
