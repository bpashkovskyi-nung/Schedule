# Schedule (NUNG)

Платформа для перегляду розкладу занять НУНГ із legacy MVC-сайтом, Android-додатком і єдиним API.

Цільова архітектура:

- `src/Schedule.Api` — ASP.NET Core API, яке є єдиним шаром інтеграції з `Dekanat.ScheduleSdk`;
- `src/Schedule.Contracts` — DTO та enum-и HTTP API;
- `src/Schedule.Client.Core` — спільний typed API client і client state;
- `Schedule.Web` — тонкий legacy MVC-клієнт, який звертається тільки до API;
- `src/Schedule.Mobile` — .NET MAUI Blazor Hybrid для Android, який звертається тільки до API.

API може використовувати `Dekanat` або `Database` як джерело розкладу. MVC і Android не знають, яке джерело активне.

## Рішення

- `Schedule.sln` — класичний Visual Studio solution
- `Schedule.slnx` — XML solution (створюється `dotnet new sln` за замовчуванням у .NET SDK 10)

Проєкти:

- `Schedule.Web/Schedule.Web.csproj`
- `tests/Schedule.Web.UnitTests/Schedule.Web.UnitTests.csproj`
- `src/Schedule.Api/Schedule.Api.csproj`
- `src/Schedule.Contracts/Schedule.Contracts.csproj`
- `src/Schedule.Client.Core/Schedule.Client.Core.csproj`
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
Зовнішні запити мають timeout/retry policy, workload endpoint-и обмежені rate limit-ом, а OpenAPI доступний у Development або коли `ScheduleApi:EnableOpenApi=true`.

## Запуск legacy MVC

В іншому терміналі:

```bash
dotnet run --project Schedule.Web/Schedule.Web.csproj
```

MVC використовує `ApiBaseUrl` із `Schedule.Web/appsettings.json`.
Усі форми залишаються GET-запитами, тому параметри видно в URL, а F5 повторює пошук.

## iCal

Legacy MVC передає iCal-запит до API. Генерація iCal і вибір джерела даних виконуються централізовано в API.

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

