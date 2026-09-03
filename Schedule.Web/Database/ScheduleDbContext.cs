using Microsoft.EntityFrameworkCore;

namespace Schedule.Web.Database;

public sealed class ScheduleDbContext(DbContextOptions<ScheduleDbContext> options) : DbContext(options)
{
    internal DbSet<ScheduleDepartmentRecord> Departments => Set<ScheduleDepartmentRecord>();

    internal DbSet<ScheduleEntityRecord> Entities => Set<ScheduleEntityRecord>();

    internal DbSet<ScheduleBuildingRecord> Buildings => Set<ScheduleBuildingRecord>();

    internal DbSet<ScheduleRoomRecord> Rooms => Set<ScheduleRoomRecord>();

    internal DbSet<ScheduleLessonRecord> Lessons => Set<ScheduleLessonRecord>();

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
            entity.HasMany(x => x.Entities)
                .WithOne(x => x.Department)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ScheduleEntityRecord>(entity =>
        {
            entity.ToTable("schedule_entities");
            entity.HasKey(x => x.Id).HasName("pk_schedule_entities");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(16).IsRequired();
            entity.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.DepartmentId).HasColumnName("department_id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(512).IsRequired();
            entity.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(256);
            entity.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(256);
            entity.Property(x => x.Patronymic).HasColumnName("patronymic").HasMaxLength(256);
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(x => new { x.TargetType, x.ExternalId }).IsUnique();
            entity.HasIndex(x => new { x.DepartmentId, x.SortOrder });
        });

        modelBuilder.Entity<ScheduleBuildingRecord>(entity =>
        {
            entity.ToTable("schedule_buildings");
            entity.HasKey(x => x.Id).HasName("pk_schedule_buildings");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(512).IsRequired();
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasMany(x => x.Rooms)
                .WithOne(x => x.Building)
                .HasForeignKey(x => x.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.HasIndex(x => new { x.BuildingId, x.SortOrder });
        });

        modelBuilder.Entity<ScheduleLessonRecord>(entity =>
        {
            entity.ToTable("schedule_lessons");
            entity.HasKey(x => x.Id).HasName("pk_schedule_lessons");
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(16).IsRequired();
            entity.Property(x => x.ObjectId).HasColumnName("object_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.ObjectName).HasColumnName("object_name").HasMaxLength(512);
            entity.Property(x => x.Date).HasColumnName("lesson_date").HasColumnType("date").IsRequired();
            entity.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(512);
            entity.Property(x => x.LessonNumber).HasColumnName("lesson_number").HasMaxLength(32);
            entity.Property(x => x.LessonName).HasColumnName("lesson_name").HasMaxLength(256);
            entity.Property(x => x.LessonTime).HasColumnName("lesson_time").HasMaxLength(64);
            entity.Property(x => x.LessonDescription).HasColumnName("lesson_description").HasMaxLength(4000);
            entity.Property(x => x.Half).HasColumnName("half").HasMaxLength(64);
            entity.Property(x => x.Teacher).HasColumnName("teacher").HasMaxLength(1000);
            entity.Property(x => x.AdditionalTeachers).HasColumnName("additional_teachers").HasMaxLength(1000);
            entity.Property(x => x.Room).HasColumnName("room").HasMaxLength(512);
            entity.Property(x => x.Group).HasColumnName("group").HasMaxLength(2000);
            entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(1000);
            entity.Property(x => x.Type).HasColumnName("type").HasMaxLength(256);
            entity.Property(x => x.Replacement).HasColumnName("replacement").HasMaxLength(2000);
            entity.Property(x => x.Reservation).HasColumnName("reservation").HasMaxLength(2000);
            entity.Property(x => x.Online).HasColumnName("online").HasMaxLength(256);
            entity.Property(x => x.LinkComment).HasColumnName("link_comment").HasMaxLength(2000);
            entity.Property(x => x.Link).HasColumnName("link").HasMaxLength(2000);
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(x => new { x.TargetType, x.ObjectId, x.Date, x.SortOrder });
        });
    }
}
