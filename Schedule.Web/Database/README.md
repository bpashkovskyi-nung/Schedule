# Тимчасове джерело розкладу з БД

Legacy підтримує два джерела даних:

- `Dekanat` — поточний запит до `timetable_export.cgi`;
- `Database` — читання довідників і пар із SQL Server.

## Перемикання

У `appsettings.json` або в змінних середовища:

```json
{
  "ScheduleSource": {
    "Provider": "Database",
    "ConnectionStringName": "ScheduleDatabase"
  },
  "ConnectionStrings": {
    "ScheduleDatabase": "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True"
  }
}
```

Щоб повернутися до деканату, достатньо змінити `Provider` на `Dekanat`. Контролери, view-моделі та генератор iCal при цьому не змінюються.

## Таблиці

Виконати `ScheduleDatabase.sql` у SQL Server. Таблиці відповідають полям моделей `Dekanat.ScheduleSdk`:

- `schedule_departments` — кафедри/підрозділи викладачів і груп;
- `schedule_entities` — викладачі та групи з їхніми деканатними ID;
- `schedule_buildings` — корпуси;
- `schedule_rooms` — аудиторії та їхні деканатні ID;
- `schedule_lessons` — рядки розкладу для груп, викладачів або аудиторій.

`schedule_lessons.object_id` має містити той самий ID, що й `schedule_entities.external_id` або `schedule_rooms.external_id`. Дата зберігається як SQL Server `date`, а під час мапінгу повертається у форматі деканату `dd.MM.yyyy`.

Репозиторій є read-only: наповнення/оновлення таблиць можна робити окремим імпортером із JSON або SQL-скриптами, не змінюючи legacy-застосунок.
