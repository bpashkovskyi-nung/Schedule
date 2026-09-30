/*
    Recreates only the schedule tables in the configured database.

    This script is intentionally explicit and destructive for the schedule
    tables. Take a backup and review the target database before running it.
    It does not drop or recreate the database itself.

    The resulting model keeps one canonical lesson and links it to lookup
    records for groups, teachers and rooms. This prevents the same lesson
    from being stored three times just because it can be requested by three
    different object types.
*/

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.schedule_lesson_rooms', N'U') IS NOT NULL
        DROP TABLE dbo.schedule_lesson_rooms;

    IF OBJECT_ID(N'dbo.schedule_lesson_teachers', N'U') IS NOT NULL
        DROP TABLE dbo.schedule_lesson_teachers;

    IF OBJECT_ID(N'dbo.schedule_lesson_groups', N'U') IS NOT NULL
        DROP TABLE dbo.schedule_lesson_groups;

    IF OBJECT_ID(N'dbo.schedule_lessons', N'U') IS NOT NULL
        DROP TABLE dbo.schedule_lessons;

    IF OBJECT_ID(N'dbo.schedule_rooms', N'U') IS NOT NULL
        DROP TABLE dbo.schedule_rooms;

    IF OBJECT_ID(N'dbo.schedule_buildings', N'U') IS NOT NULL
        DROP TABLE dbo.schedule_buildings;

    IF OBJECT_ID(N'dbo.schedule_teachers', N'U') IS NOT NULL
        DROP TABLE dbo.schedule_teachers;

    IF OBJECT_ID(N'dbo.schedule_groups', N'U') IS NOT NULL
        DROP TABLE dbo.schedule_groups;

    IF OBJECT_ID(N'dbo.schedule_entities', N'U') IS NOT NULL
        DROP TABLE dbo.schedule_entities;

    IF OBJECT_ID(N'dbo.schedule_sync_runs', N'U') IS NOT NULL
        DROP TABLE dbo.schedule_sync_runs;

    IF OBJECT_ID(N'dbo.schedule_departments', N'U') IS NOT NULL
        DROP TABLE dbo.schedule_departments;

    CREATE TABLE dbo.schedule_departments
    (
        id          int IDENTITY(1, 1) NOT NULL,
        target_type nvarchar(16) NOT NULL,
        name        nvarchar(512) NOT NULL,
        sort_order  int NOT NULL CONSTRAINT df_schedule_departments_sort_order DEFAULT (0),
        CONSTRAINT pk_schedule_departments PRIMARY KEY (id),
        CONSTRAINT uq_schedule_departments_target_name UNIQUE (target_type, name)
    );

    CREATE TABLE dbo.schedule_sync_runs
    (
        id               bigint IDENTITY(1, 1) NOT NULL,
        from_date        date NOT NULL,
        to_date          date NOT NULL,
        started_at_utc   datetime2(0) NOT NULL,
        completed_at_utc datetime2(0) NULL,
        status           nvarchar(32) NOT NULL,
        error            nvarchar(4000) NULL,
        CONSTRAINT pk_schedule_sync_runs PRIMARY KEY (id)
    );

    CREATE TABLE dbo.schedule_groups
    (
        id            bigint IDENTITY(1, 1) NOT NULL,
        external_id   nvarchar(128) NOT NULL,
        department_id int NULL,
        name          nvarchar(512) NOT NULL,
        sort_order    int NOT NULL CONSTRAINT df_schedule_groups_sort_order DEFAULT (0),
        CONSTRAINT pk_schedule_groups PRIMARY KEY (id),
        CONSTRAINT uq_schedule_groups_external_id UNIQUE (external_id),
        CONSTRAINT fk_schedule_groups_department
            FOREIGN KEY (department_id) REFERENCES dbo.schedule_departments (id)
            ON DELETE SET NULL
    );

    CREATE TABLE dbo.schedule_teachers
    (
        id            bigint IDENTITY(1, 1) NOT NULL,
        external_id   nvarchar(128) NOT NULL,
        department_id int NULL,
        name          nvarchar(512) NOT NULL,
        last_name     nvarchar(256) NULL,
        first_name    nvarchar(256) NULL,
        patronymic    nvarchar(256) NULL,
        sort_order    int NOT NULL CONSTRAINT df_schedule_teachers_sort_order DEFAULT (0),
        CONSTRAINT pk_schedule_teachers PRIMARY KEY (id),
        CONSTRAINT uq_schedule_teachers_external_id UNIQUE (external_id),
        CONSTRAINT fk_schedule_teachers_department
            FOREIGN KEY (department_id) REFERENCES dbo.schedule_departments (id)
            ON DELETE SET NULL
    );

    CREATE TABLE dbo.schedule_buildings
    (
        id         int IDENTITY(1, 1) NOT NULL,
        name       nvarchar(512) NOT NULL,
        sort_order int NOT NULL CONSTRAINT df_schedule_buildings_sort_order DEFAULT (0),
        CONSTRAINT pk_schedule_buildings PRIMARY KEY (id),
        CONSTRAINT uq_schedule_buildings_name UNIQUE (name)
    );

    CREATE TABLE dbo.schedule_rooms
    (
        id          bigint IDENTITY(1, 1) NOT NULL,
        external_id nvarchar(128) NOT NULL,
        name        nvarchar(512) NOT NULL,
        building_id int NOT NULL,
        sort_order  int NOT NULL CONSTRAINT df_schedule_rooms_sort_order DEFAULT (0),
        CONSTRAINT pk_schedule_rooms PRIMARY KEY (id),
        CONSTRAINT uq_schedule_rooms_external_id UNIQUE (external_id),
        CONSTRAINT fk_schedule_rooms_building
            FOREIGN KEY (building_id) REFERENCES dbo.schedule_buildings (id)
            ON DELETE NO ACTION
    );

    CREATE TABLE dbo.schedule_lessons
    (
        id                bigint IDENTITY(1, 1) NOT NULL,
        source_key        nvarchar(128) NOT NULL,
        source_hash       nvarchar(128) NULL,
        lesson_date       date NOT NULL,
        comment           nvarchar(512) NULL,
        lesson_number     nvarchar(32) NULL,
        lesson_name       nvarchar(256) NULL,
        lesson_time       nvarchar(64) NULL,
        lesson_description nvarchar(4000) NULL,
        half              nvarchar(128) NULL,
        title             nvarchar(1000) NULL,
        type              nvarchar(256) NULL,
        replacement       nvarchar(2000) NULL,
        reservation       nvarchar(2000) NULL,
        online            nvarchar(256) NULL,
        link_comment      nvarchar(2000) NULL,
        link              nvarchar(2000) NULL,
        sort_order        int NOT NULL CONSTRAINT df_schedule_lessons_sort_order DEFAULT (0),
        last_sync_run_id  bigint NULL,
        CONSTRAINT pk_schedule_lessons PRIMARY KEY (id),
        CONSTRAINT uq_schedule_lessons_source_key UNIQUE (source_key),
        CONSTRAINT fk_schedule_lessons_sync_run
            FOREIGN KEY (last_sync_run_id) REFERENCES dbo.schedule_sync_runs (id)
            ON DELETE SET NULL
    );

    CREATE TABLE dbo.schedule_lesson_groups
    (
        lesson_id bigint NOT NULL,
        group_id  bigint NOT NULL,
        subgroup  nvarchar(128) NOT NULL,
        CONSTRAINT pk_schedule_lesson_groups PRIMARY KEY (lesson_id, group_id, subgroup),
        CONSTRAINT fk_schedule_lesson_groups_lesson
            FOREIGN KEY (lesson_id) REFERENCES dbo.schedule_lessons (id)
            ON DELETE CASCADE,
        CONSTRAINT fk_schedule_lesson_groups_group
            FOREIGN KEY (group_id) REFERENCES dbo.schedule_groups (id)
            ON DELETE CASCADE
    );

    CREATE TABLE dbo.schedule_lesson_teachers
    (
        lesson_id  bigint NOT NULL,
        teacher_id bigint NOT NULL,
        is_primary bit NOT NULL CONSTRAINT df_schedule_lesson_teachers_is_primary DEFAULT (0),
        CONSTRAINT pk_schedule_lesson_teachers PRIMARY KEY (lesson_id, teacher_id),
        CONSTRAINT fk_schedule_lesson_teachers_lesson
            FOREIGN KEY (lesson_id) REFERENCES dbo.schedule_lessons (id)
            ON DELETE CASCADE,
        CONSTRAINT fk_schedule_lesson_teachers_teacher
            FOREIGN KEY (teacher_id) REFERENCES dbo.schedule_teachers (id)
            ON DELETE CASCADE
    );

    CREATE TABLE dbo.schedule_lesson_rooms
    (
        lesson_id bigint NOT NULL,
        room_id   bigint NOT NULL,
        is_primary bit NOT NULL CONSTRAINT df_schedule_lesson_rooms_is_primary DEFAULT (0),
        CONSTRAINT pk_schedule_lesson_rooms PRIMARY KEY (lesson_id, room_id),
        CONSTRAINT fk_schedule_lesson_rooms_lesson
            FOREIGN KEY (lesson_id) REFERENCES dbo.schedule_lessons (id)
            ON DELETE CASCADE,
        CONSTRAINT fk_schedule_lesson_rooms_room
            FOREIGN KEY (room_id) REFERENCES dbo.schedule_rooms (id)
            ON DELETE CASCADE
    );

    CREATE INDEX ix_schedule_groups_department_id
        ON dbo.schedule_groups (department_id);

    CREATE INDEX ix_schedule_teachers_department_id
        ON dbo.schedule_teachers (department_id);

    CREATE INDEX ix_schedule_rooms_building_id
        ON dbo.schedule_rooms (building_id);

    CREATE INDEX ix_schedule_lessons_date_lesson_number
        ON dbo.schedule_lessons (lesson_date, lesson_number);

    CREATE INDEX ix_schedule_lessons_last_sync_run_id
        ON dbo.schedule_lessons (last_sync_run_id);

    CREATE INDEX ix_schedule_lesson_groups_group_lesson
        ON dbo.schedule_lesson_groups (group_id, lesson_id);

    CREATE INDEX ix_schedule_lesson_teachers_teacher_lesson
        ON dbo.schedule_lesson_teachers (teacher_id, lesson_id);

    CREATE INDEX ix_schedule_lesson_rooms_room_lesson
        ON dbo.schedule_lesson_rooms (room_id, lesson_id);

    CREATE INDEX ix_schedule_sync_runs_period_started
        ON dbo.schedule_sync_runs (from_date, to_date, started_at_utc);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
