# Legacy MVC-клієнт розкладу

Тонкий ASP.NET Core MVC-клієнт. Він не підключається напряму до деканату або SQL Server, а отримує всі дані через `Schedule.Api`.

## Функції

- Розклад за факультетом / викладачем / групою
- Розклад аудиторій, груп, викладачів
- Завантаження аудиторій та навчальне навантаження
- Список і тижневий вигляд
- Експорт iCal

## Архітектура

- ASP.NET Core MVC (.NET 10)
- `Schedule.Client.Core` — HTTP-клієнт `Schedule.Api`
- `Schedule.Contracts` — DTO HTTP API
- Bootstrap 5

Єдине місце інтеграції з Dekanat SDK та БД — `src/Schedule.Api`. Джерело даних перемикається конфігурацією API, а legacy залишається незмінним.

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
├── Services/              # тонкі адаптери API та підготовка даних для view
├── ViewModels/
├── Views/Home/Index.cshtml
└── wwwroot/css, js
```
