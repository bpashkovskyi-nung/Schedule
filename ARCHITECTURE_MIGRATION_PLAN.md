# План і стан перебудови архітектури

## Цільова схема

```text
Legacy MVC ───┐
Android ──────┼── Schedule.Api ── Dekanat або БД
```

У системі залишається один веб-клієнт — поточний ASP.NET Core MVC. Окремий Blazor Web більше не потрібен і не входить до solution.

## Відповідальність проєктів

### `src/Schedule.Api`

Єдиний серверний шар і єдине місце, де дозволені:

- `Dekanat.ScheduleSdk`;
- Entity Framework Core та SQL Server;
- перемикання джерела `Dekanat` / `Database`;
- нормалізація деканатних моделей;
- генерація iCal;
- обчислення навантаження аудиторій і викладачів;
- кешування довідників, rate limit і обробка помилок зовнішнього API.

API має зберігати однакову поведінку клієнтів незалежно від активного джерела даних. За це відповідає `IScheduleDataSource` з двома реалізаціями:

- `DekanatScheduleDataSource` — запити через SDK;
- `DatabaseScheduleDataSource` — читання локальної БД через EF Core.

### `src/Schedule.Contracts`

Містить тільки DTO та enum-и публічного HTTP API:

- довідники;
- розклад;
- підказки;
- навантаження;
- помилки та діапазони дат.

Контракти не повинні залежати від ASP.NET Core, MAUI, MVC або `Dekanat.ScheduleSdk`. Поля, які не читають клієнти, не додаються до контрактів. SDK-моделі використовуються всередині API та адаптерів джерел, а не дублюються в MVC чи Android.

### `src/Schedule.Client.Core`

Спільний typed HTTP-клієнт і стан клієнта:

- `IScheduleApiClient` / `ScheduleApiClient`;
- єдина побудова URL та серіалізація дат;
- обробка помилок API;
- `ReferenceDataState` і `ScheduleState` для Android.

Цей проєкт не знає про Dekanat, SQL Server, EF Core або MVC.

### `Schedule.Web`

Тонкий legacy MVC-клієнт:

- отримує довідники через `Schedule.Client.Core`;
- передає пошукові параметри до API;
- перетворює API DTO у старі ViewModel, потрібні Razor view;
- зберігає GET-маршрути та поведінку F5;
- перенаправляє iCal на API;
- не містить SDK, EF Core, connection string, SQL, локального репозиторію чи власного генератора iCal.

Допускається локальна підготовка даних лише для відображення: групування по даті, форматування дат, посилань і тексту для view.

### `src/Schedule.Mobile`

Тонкий .NET MAUI Blazor Hybrid-клієнт для Android:

- звертається до `Schedule.Api` через `Schedule.Client.Core`;
- зберігає вибір користувача та локальні налаштування на пристрої;
- має native-специфічні функції: Android notifications, secure/local storage тощо;
- не підключає `Dekanat.ScheduleSdk` і не має прямого доступу до БД.

UI-компоненти, які потрібні лише Android, зберігаються в Android-проєкті. Окремий `Schedule.Client.UI` вилучено, оскільки спільного веб-клієнта більше немає.

## HTTP-поверхня API

Основні маршрути:

```text
GET /api/v1/reference
GET /api/v1/schedules/{teacher|group|room}/{objectId}?from=yyyy-MM-dd&to=yyyy-MM-dd
GET /api/v1/schedules/{teacher|group|room}/{objectId}/ical?from=yyyy-MM-dd&to=yyyy-MM-dd
GET /api/v1/suggestions/teachers
GET /api/v1/suggestions/groups
GET /api/v1/workload/teachers
GET /api/v1/workload/auditoriums
```

Клієнти не формують URL деканату. Для них єдина точка входу — `ApiBaseUrl` або мобільна конфігурація API.

## Перемикання джерела

Перемикання робиться тільки в конфігурації `Schedule.Api`:

```json
{
  "ScheduleSource": {
    "Provider": "Dekanat",
    "ConnectionStringName": "ScheduleDatabase"
  }
}
```

Допустимі значення `Provider` — `Dekanat` і `Database`. Connection string зберігається тільки в конфігурації API або в секретах середовища. У `Schedule.Web` його бути не повинно.

## Виконані зміни

- [x] API є єдиним шаром інтеграції з Dekanat і SQL Server.
- [x] Legacy MVC переведено на HTTP API.
- [x] Legacy MVC зберіг GET-форми, URL-параметри та F5-сценарій.
- [x] Legacy iCal перенесено на API.
- [x] Android використовує API через `Schedule.Client.Core`.
- [x] Видалено прямі SDK/EF/DB-залежності з MVC.
- [x] Видалено локальні MVC-репозиторії, локальний iCal-генератор і старий DB schema-файл.
- [x] Видалено окремий Blazor Web та `Schedule.Client.UI` із solution.
- [x] Із `Schedule.Contracts` видалено невикористовувані обчислювані поля типу пари/годин.
- [x] Перевірено `Schedule.sln` і `Schedule.slnx` — у них немає нового веба.

## Перевірка готовності

```powershell
dotnet build Schedule.sln --no-restore
dotnet test Schedule.sln --no-build --no-restore
dotnet build src/Schedule.Mobile/Schedule.Mobile.csproj --no-restore -f net10.0-android
```

Перед релізом окремо перевірити API з кожним значенням `ScheduleSource:Provider`, а також iCal, довідники, розклад групи/викладача/аудиторії та обидва workload endpoint-и.

## Наступні задачі

1. Розгорнути оновлений `Schedule.Api` на production URL.
2. Перевірити connection string API на сервері, не додаючи його до MVC або Android.
3. Протестувати production API з legacy та Android.
4. За потреби додати API integration tests з mock-джерелами `IScheduleDataSource`.
