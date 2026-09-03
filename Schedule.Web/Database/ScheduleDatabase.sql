/*
   SQL Server schema for the temporary legacy schedule source.

   Values in schedule_departments.target_type and schedule_lessons.target_type:
     teacher, group, room

   The data is intentionally close to Dekanat.ScheduleSdk.Models so the web
   application can map rows back to PsRozkladExport without changing views.
*/

IF OBJECT_ID(N'dbo.schedule_departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.schedule_departments
    (
        id          int IDENTITY(1, 1) NOT NULL,
        target_type varchar(16)        NOT NULL,
        name        nvarchar(512)      NOT NULL,
        sort_order  int                NOT NULL CONSTRAINT df_schedule_departments_sort_order DEFAULT (0),

        CONSTRAINT pk_schedule_departments PRIMARY KEY (id),
        CONSTRAINT ck_schedule_departments_target_type CHECK (target_type IN ('teacher', 'group')),
        CONSTRAINT uq_schedule_departments_target_name UNIQUE (target_type, name)
    );
END;
GO

IF OBJECT_ID(N'dbo.schedule_entities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.schedule_entities
    (
        id            bigint IDENTITY(1, 1) NOT NULL,
        target_type   varchar(16)           NOT NULL,
        external_id   nvarchar(128)         NOT NULL,
        department_id int                   NULL,
        name          nvarchar(512)         NOT NULL,
        last_name     nvarchar(256)         NULL,
        first_name    nvarchar(256)         NULL,
        patronymic    nvarchar(256)         NULL,
        sort_order    int                   NOT NULL CONSTRAINT df_schedule_entities_sort_order DEFAULT (0),

        CONSTRAINT pk_schedule_entities PRIMARY KEY (id),
        CONSTRAINT ck_schedule_entities_target_type CHECK (target_type IN ('teacher', 'group')),
        CONSTRAINT fk_schedule_entities_department
            FOREIGN KEY (department_id) REFERENCES dbo.schedule_departments (id) ON DELETE CASCADE,
        CONSTRAINT uq_schedule_entities_target_external UNIQUE (target_type, external_id)
    );

    CREATE INDEX ix_schedule_entities_department_sort
        ON dbo.schedule_entities (department_id, sort_order, name);
END;
GO

IF OBJECT_ID(N'dbo.schedule_buildings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.schedule_buildings
    (
        id         int IDENTITY(1, 1) NOT NULL,
        name       nvarchar(512)      NOT NULL,
        sort_order int                NOT NULL CONSTRAINT df_schedule_buildings_sort_order DEFAULT (0),

        CONSTRAINT pk_schedule_buildings PRIMARY KEY (id),
        CONSTRAINT uq_schedule_buildings_name UNIQUE (name)
    );
END;
GO

IF OBJECT_ID(N'dbo.schedule_rooms', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.schedule_rooms
    (
        id          bigint IDENTITY(1, 1) NOT NULL,
        external_id nvarchar(128)         NOT NULL,
        name        nvarchar(512)         NOT NULL,
        building_id int                   NOT NULL,
        sort_order  int                   NOT NULL CONSTRAINT df_schedule_rooms_sort_order DEFAULT (0),

        CONSTRAINT pk_schedule_rooms PRIMARY KEY (id),
        CONSTRAINT fk_schedule_rooms_building
            FOREIGN KEY (building_id) REFERENCES dbo.schedule_buildings (id) ON DELETE CASCADE,
        CONSTRAINT uq_schedule_rooms_external_id UNIQUE (external_id)
    );

    CREATE INDEX ix_schedule_rooms_building_sort
        ON dbo.schedule_rooms (building_id, sort_order, name);
END;
GO

IF OBJECT_ID(N'dbo.schedule_lessons', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.schedule_lessons
    (
        id                  bigint IDENTITY(1, 1) NOT NULL,
        target_type         varchar(16)           NOT NULL,
        object_id           nvarchar(128)         NOT NULL,
        object_name         nvarchar(512)         NULL,
        lesson_date         date                  NOT NULL,
        comment             nvarchar(512)         NULL,
        lesson_number       nvarchar(32)          NULL,
        lesson_name         nvarchar(256)         NULL,
        lesson_time         nvarchar(64)          NULL,
        lesson_description  nvarchar(4000)        NULL,
        half                nvarchar(64)          NULL,
        teacher             nvarchar(1000)        NULL,
        additional_teachers nvarchar(1000)        NULL,
        room                nvarchar(512)         NULL,
        [group]             nvarchar(2000)        NULL,
        title               nvarchar(1000)        NULL,
        [type]              nvarchar(256)         NULL,
        replacement         nvarchar(2000)        NULL,
        reservation         nvarchar(2000)        NULL,
        online              nvarchar(256)         NULL,
        link_comment        nvarchar(2000)        NULL,
        link                nvarchar(2000)        NULL,
        sort_order          int                   NOT NULL CONSTRAINT df_schedule_lessons_sort_order DEFAULT (0),

        CONSTRAINT pk_schedule_lessons PRIMARY KEY (id),
        CONSTRAINT ck_schedule_lessons_target_type CHECK (target_type IN ('teacher', 'group', 'room'))
    );

    CREATE INDEX ix_schedule_lessons_target_period
        ON dbo.schedule_lessons (target_type, object_id, lesson_date, sort_order);
END;
GO
