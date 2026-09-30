using Microsoft.EntityFrameworkCore;

namespace Schedule.Api.Database;

internal sealed class ScheduleDbContext(DbContextOptions<ScheduleDbContext> options) : DbContext(options)
{
    internal DbSet<ScheduleDepartmentRecord> Departments => Set<ScheduleDepartmentRecord>();
    internal DbSet<ScheduleGroupRecord> Groups => Set<ScheduleGroupRecord>();
    internal DbSet<ScheduleTeacherRecord> Teachers => Set<ScheduleTeacherRecord>();
    internal DbSet<ScheduleBuildingRecord> Buildings => Set<ScheduleBuildingRecord>();
    internal DbSet<ScheduleRoomRecord> Rooms => Set<ScheduleRoomRecord>();
    internal DbSet<ScheduleLessonRecord> Lessons => Set<ScheduleLessonRecord>();
    internal DbSet<ScheduleLessonGroupRecord> LessonGroups => Set<ScheduleLessonGroupRecord>();
    internal DbSet<ScheduleLessonTeacherRecord> LessonTeachers => Set<ScheduleLessonTeacherRecord>();
    internal DbSet<ScheduleLessonRoomRecord> LessonRooms => Set<ScheduleLessonRoomRecord>();
    internal DbSet<ScheduleSyncRunRecord> SyncRuns => Set<ScheduleSyncRunRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScheduleDepartmentRecord>(entity =>
        {
            entity.ToTable("schedule_departments");
            entity.HasKey(x => x.Id).HasName("pk_schedule_departments");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(16).IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(512).IsRequired();
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(x => new { x.TargetType, x.Name }).IsUnique();
        });

        modelBuilder.Entity<ScheduleGroupRecord>(entity =>
        {
            entity.ToTable("schedule_groups");
            entity.HasKey(x => x.Id).HasName("pk_schedule_groups");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.DepartmentId).HasColumnName("department_id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(512).IsRequired();
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(x => x.ExternalId).IsUnique();
            entity.HasOne<ScheduleDepartmentRecord>()
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ScheduleTeacherRecord>(entity =>
        {
            entity.ToTable("schedule_teachers");
            entity.HasKey(x => x.Id).HasName("pk_schedule_teachers");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.DepartmentId).HasColumnName("department_id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(512).IsRequired();
            entity.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(256);
            entity.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(256);
            entity.Property(x => x.Patronymic).HasColumnName("patronymic").HasMaxLength(256);
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(x => x.ExternalId).IsUnique();
            entity.HasOne<ScheduleDepartmentRecord>()
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ScheduleBuildingRecord>(entity =>
        {
            entity.ToTable("schedule_buildings");
            entity.HasKey(x => x.Id).HasName("pk_schedule_buildings");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(512).IsRequired();
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<ScheduleRoomRecord>(entity =>
        {
            entity.ToTable("schedule_rooms");
            entity.HasKey(x => x.Id).HasName("pk_schedule_rooms");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(512).IsRequired();
            entity.Property(x => x.BuildingId).HasColumnName("building_id");
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(x => x.ExternalId).IsUnique();
            entity.HasOne(x => x.Building)
                .WithMany(x => x.Rooms)
                .HasForeignKey(x => x.BuildingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ScheduleLessonRecord>(entity =>
        {
            entity.ToTable("schedule_lessons");
            entity.HasKey(x => x.Id).HasName("pk_schedule_lessons");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.SourceKey).HasColumnName("source_key").HasMaxLength(128).IsRequired();
            entity.Property(x => x.SourceHash).HasColumnName("source_hash").HasMaxLength(128);
            entity.Property(x => x.Date).HasColumnName("lesson_date").HasColumnType("date").IsRequired();
            entity.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(512);
            entity.Property(x => x.LessonNumber).HasColumnName("lesson_number").HasMaxLength(32);
            entity.Property(x => x.LessonName).HasColumnName("lesson_name").HasMaxLength(256);
            entity.Property(x => x.LessonTime).HasColumnName("lesson_time").HasMaxLength(64);
            entity.Property(x => x.LessonDescription).HasColumnName("lesson_description").HasMaxLength(4000);
            entity.Property(x => x.Half).HasColumnName("half").HasMaxLength(128);
            entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(1000);
            entity.Property(x => x.Type).HasColumnName("type").HasMaxLength(256);
            entity.Property(x => x.Replacement).HasColumnName("replacement").HasMaxLength(2000);
            entity.Property(x => x.Reservation).HasColumnName("reservation").HasMaxLength(2000);
            entity.Property(x => x.Online).HasColumnName("online").HasMaxLength(256);
            entity.Property(x => x.LinkComment).HasColumnName("link_comment").HasMaxLength(2000);
            entity.Property(x => x.Link).HasColumnName("link").HasMaxLength(2000);
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.LastSyncRunId).HasColumnName("last_sync_run_id");
            entity.HasIndex(x => x.SourceKey).IsUnique();
            entity.HasIndex(x => new { x.Date, x.LessonNumber });
        });

        modelBuilder.Entity<ScheduleLessonGroupRecord>(entity =>
        {
            entity.ToTable("schedule_lesson_groups");
            entity.HasKey(x => new { x.LessonId, x.GroupId, x.Subgroup }).HasName("pk_schedule_lesson_groups");
            entity.Property(x => x.LessonId).HasColumnName("lesson_id");
            entity.Property(x => x.GroupId).HasColumnName("group_id");
            entity.Property(x => x.Subgroup).HasColumnName("subgroup").HasMaxLength(128).IsRequired();
            entity.HasOne(x => x.Lesson)
                .WithMany(x => x.Groups)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Group)
                .WithMany(x => x.LessonLinks)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.GroupId, x.LessonId });
        });

        modelBuilder.Entity<ScheduleLessonTeacherRecord>(entity =>
        {
            entity.ToTable("schedule_lesson_teachers");
            entity.HasKey(x => new { x.LessonId, x.TeacherId }).HasName("pk_schedule_lesson_teachers");
            entity.Property(x => x.LessonId).HasColumnName("lesson_id");
            entity.Property(x => x.TeacherId).HasColumnName("teacher_id");
            entity.Property(x => x.IsPrimary).HasColumnName("is_primary");
            entity.HasOne(x => x.Lesson)
                .WithMany(x => x.Teachers)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Teacher)
                .WithMany(x => x.LessonLinks)
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.TeacherId, x.LessonId });
        });

        modelBuilder.Entity<ScheduleLessonRoomRecord>(entity =>
        {
            entity.ToTable("schedule_lesson_rooms");
            entity.HasKey(x => new { x.LessonId, x.RoomId }).HasName("pk_schedule_lesson_rooms");
            entity.Property(x => x.LessonId).HasColumnName("lesson_id");
            entity.Property(x => x.RoomId).HasColumnName("room_id");
            entity.Property(x => x.IsPrimary).HasColumnName("is_primary");
            entity.HasOne(x => x.Lesson)
                .WithMany(x => x.Rooms)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Room)
                .WithMany(x => x.LessonLinks)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.RoomId, x.LessonId });
        });

        modelBuilder.Entity<ScheduleSyncRunRecord>(entity =>
        {
            entity.ToTable("schedule_sync_runs");
            entity.HasKey(x => x.Id).HasName("pk_schedule_sync_runs");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.FromDate).HasColumnName("from_date").HasColumnType("date");
            entity.Property(x => x.ToDate).HasColumnName("to_date").HasColumnType("date");
            entity.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc").IsRequired();
            entity.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(x => x.Error).HasColumnName("error").HasMaxLength(4000);
            entity.HasIndex(x => new { x.FromDate, x.ToDate, x.StartedAtUtc });
        });
    }
}
