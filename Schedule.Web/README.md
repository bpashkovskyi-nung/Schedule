# Legacy MVC-клієнт розкладу

Цей проєкт тимчасово залишений для безпечної міграції. Новий клієнт розташований у `src/Schedule.Web` і працює через `src/Schedule.Api`.

Додаток для перегляду розкладу занять НУНГ на **ASP.NET Core MVC** з інтеграцією [Dekanat.ScheduleSdk](https://www.nuget.org/packages/Dekanat.ScheduleSdk).

## Функції

- Розклад за факультетом / викладачем / групою
- Розклад аудиторій, груп, викладачів
- Завантаження аудиторій та навчальне навантаження
- Список і тижневий вигляд
- Експорт iCal

## Технології

- ASP.NET Core MVC (.NET 10)
- [Dekanat.ScheduleSdk](https://www.nuget.org/packages/Dekanat.ScheduleSdk) — API ПС-Розклад
- Bootstrap 5

## Запуск

```bash
cd Schedule.Web
dotnet run
```

Відкрийте URL з `launchSettings.json` (зазвичай `https://localhost:7xxx`).

## Структура

```
Schedule.Web/
├── Controllers/HomeController.cs
├── Services/              # робота з SDK та підказками timetable.cgi
├── ViewModels/
├── Views/Home/Index.cshtml
└── wwwroot/css, js
```
