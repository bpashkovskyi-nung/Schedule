# Schedule (NUNG)

Платформа для перегляду розкладу занять НУНГ із окремим API та спільним клієнтським UI для Web і Android.

Поточна міграція додає цільову структуру:

- `src/Schedule.Api` — ASP.NET Core API, яке є єдиним шаром інтеграції з `Dekanat.ScheduleSdk`;
- `src/Schedule.Contracts` — DTO та enum-и HTTP API;
- `src/Schedule.Client.Core` — спільний typed API client і client state;
- `src/Schedule.Client.UI` — спільна Razor Class Library;
- `src/Schedule.Web` — Blazor Web App з Interactive WebAssembly;
- `src/Schedule.Mobile` — .NET MAUI Blazor Hybrid для Android.

Старий `Schedule.Web` у корені рішення тимчасово зберігається як legacy MVC-клієнт на час міграції та згрупований у solution-папці `legacy`.

## Рішення

- `Schedule.sln` — класичний Visual Studio solution
- `Schedule.slnx` — XML solution (створюється `dotnet new sln` за замовчуванням у .NET SDK 10)

Legacy та нові проєкти:

- `Schedule.Web/Schedule.Web.csproj`
- `Schedule.UnitTests/Schedule.UnitTests.csproj`
- `src/Schedule.Api/Schedule.Api.csproj`
- `src/Schedule.Contracts/Schedule.Contracts.csproj`
- `src/Schedule.Client.Core/Schedule.Client.Core.csproj`
- `src/Schedule.Client.UI/Schedule.Client.UI.csproj`
- `src/Schedule.Web/Schedule.Web/Schedule.Web.csproj`
- `src/Schedule.Mobile/Schedule.Mobile.csproj`
- `tests/Schedule.Api.UnitTests/Schedule.Api.UnitTests.csproj`
- `tests/Schedule.Client.Core.UnitTests/Schedule.Client.Core.UnitTests.csproj`

## Вимоги

- .NET SDK **10.0.201** (або сумісний .NET 10)

## Запуск API

```bash
dotnet run --project src/Schedule.Api
```

API після запуску доступний на `http://localhost:5274`.

API кешує довідники на 10 хвилин і повертає помилки клієнтам у форматі
`{ "code": "...", "message": "...", "traceId": "..." }`.

## Запуск нового Web-клієнта

В іншому терміналі:

```bash
dotnet run --project src/Schedule.Web/Schedule.Web
```

Web-клієнт використовує `ApiBaseUrl` із `src/Schedule.Web/Schedule.Web/appsettings.Development.json`.
Спільна сторінка розкладу підтримує список занять і тижневий вигляд.

## Запуск Android

```bash
dotnet build src/Schedule.Mobile/Schedule.Mobile.csproj -f net10.0-android
```

## Тести

```bash
dotnet test Schedule.slnx
```

## Ліцензія

MIT — див. `LICENSE`.

