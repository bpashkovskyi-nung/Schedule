# План оновлення архітектури Schedule

## 1. Мета міграції

Перетворити поточний ASP.NET Core MVC-проєкт на рішення з трьома окремими частинами:

1. `Schedule.Api` — єдиний backend/API для роботи з розкладом НУНГ.
2. `Schedule.Web` — Blazor Web App для браузера.
3. `Schedule.Mobile` — .NET MAUI Blazor Hybrid-застосунок для Android.

Спільний код клієнтів має бути винесений у:

- `Schedule.Contracts` — DTO, enum-и та контракти HTTP API;
- `Schedule.Client.Core` — спільний API-клієнт, бізнес-логіка, стан і кешування;
- `Schedule.Client.UI` — Razor Class Library зі спільними сторінками, компонентами та layout.

Ключовий результат: Web і Android використовують однакові Razor-компоненти та клієнтську логіку, а жоден клієнт не звертається безпосередньо до `Dekanat.ScheduleSdk` або `timetable.cgi`.

## Статус виконання

Стан на 29.08.2026 у гілці `codex/schedule-api-blazor-maui`:

- [x] Створено `Schedule.Api`, `Schedule.Contracts`, `Schedule.Client.Core`, `Schedule.Client.UI`, `Schedule.Web` і `Schedule.Mobile`.
- [x] Винесено SDK-інтеграцію в API та додано endpoint-и розкладу, довідників, suggestions і workload.
- [x] Додано спільний typed HTTP client, state-сервіси та Razor UI для Web/Android.
- [x] Додано кеш довідників API та єдиний формат помилок для клієнтів.
- [x] Додано unit-тести API-розрахунків і HTTP-клієнта.
- [x] Legacy MVC залишено ізольованим для безпечного переходу; він ще присутній у solution і буде видалений після приймання нових клієнтів.
- [ ] Додати production URL/API deployment settings, Android signing, notifications і platform-specific storage.
- [ ] Перенести або видалити legacy MVC після перевірки нової Web/Android версії.

---

## 2. Поточний стан проєкту

Зараз рішення містить:

```text
Schedule.sln
├─ Schedule.Web              ← ASP.NET Core MVC-застосунок
└─ Schedule.UnitTests        ← unit-тести
```

Поточний `Schedule.Web` одночасно виконує роль UI та backend-шару:

- `Program.cs` реєструє `Dekanat.ScheduleSdk` напряму;
- `HomeController` приймає форми та виконує пошук розкладу;
- `ScheduleQueryService` напряму викликає `IPsRozkladClient`;
- `ScheduleReferenceService` напряму завантажує списки аудиторій, викладачів і груп;
- `TimetableSuggestionsService` напряму звертається до `timetable.cgi`;
- Razor Views відображають HTML через MVC;
- `AuditoriumLoadService` і `ScheduleDisplayService` містять частину бізнес-логіки.

Поточний проєкт ще не має окремого HTTP API для власних клієнтів. Тому міграція повинна починатися не зі створення Android-проєкту, а з виділення backend-функціональності та стабільних контрактів API.

---

## 3. Цільова структура solution

Рекомендована структура:

```text
Schedule.sln
├─ src/
│  ├─ Schedule.Api/
│  │  ├─ Controllers/
│  │  ├─ Endpoints/
│  │  ├─ Services/
│  │  ├─ Mapping/
│  │  ├─ Infrastructure/
│  │  ├─ Configuration/
│  │  └─ Program.cs
│  │
│  ├─ Schedule.Contracts/
│  │  ├─ Schedules/
│  │  ├─ ReferenceData/
│  │  ├─ Workload/
│  │  ├─ AuditoriumLoad/
│  │  ├─ Suggestions/
│  │  └─ Common/
│  │
│  ├─ Schedule.Client.Core/
│  │  ├─ Api/
│  │  ├─ Models/
│  │  ├─ Services/
│  │  ├─ State/
│  │  ├─ Storage/
│  │  └─ Configuration/
│  │
│  ├─ Schedule.Client.UI/
│  │  ├─ Pages/
│  │  ├─ Components/
│  │  ├─ Layout/
│  │  ├─ Shared/
│  │  └─ wwwroot/
│  │
│  ├─ Schedule.Web/
│  │  ├─ Layout/
│  │  ├─ wwwroot/
│  │  ├─ Program.cs
│  │  └─ appsettings.json
│  │
│  └─ Schedule.Mobile/
│     ├─ Platforms/Android/
│     ├─ Resources/
│     ├─ Services/
│     ├─ MainPage.xaml
│     ├─ MauiProgram.cs
│     └─ Schedule.Mobile.csproj
│
└─ tests/
   ├─ Schedule.Api.UnitTests/
   ├─ Schedule.Api.IntegrationTests/
   ├─ Schedule.Client.Core.Tests/
   └─ Schedule.Client.UI.Tests/
```

`Schedule.UnitTests` можна тимчасово залишити на першому етапі, але після перенесення тестів його краще перейменувати або розділити за відповідальністю.

---

## 4. Правила залежностей

Залежності мають бути спрямовані донизу, без циклічних посилань:

```text
Schedule.Api ────────────────> Schedule.Contracts
Schedule.Client.Core ────────> Schedule.Contracts
Schedule.Client.UI ──────────> Schedule.Client.Core
Schedule.Client.UI ──────────> Schedule.Contracts
Schedule.Web ────────────────> Schedule.Client.UI
Schedule.Web ────────────────> Schedule.Client.Core
Schedule.Mobile ────────────> Schedule.Client.UI
Schedule.Mobile ────────────> Schedule.Client.Core
```

Додаткові правила:

- `Schedule.Contracts` не повинен залежати від ASP.NET Core, Blazor, MAUI або Dekanat SDK;
- `Schedule.Client.Core` не повинен залежати від `Microsoft.Maui`;
- `Schedule.Client.UI` має містити тільки UI та виклики абстракцій `Client.Core`;
- `Schedule.Api` є єдиним проєктом, який залежить від `Dekanat.ScheduleSdk`;
- `Schedule.Web` і `Schedule.Mobile` не повинні містити дубльовану логіку запитів і розрахунків;
- SDK-моделі не можна віддавати назовні з API — їх необхідно мапити на власні DTO.

---

## 5. Етап 0 — зафіксувати поточну поведінку

### Завдання

- Перевірити запуск поточного MVC-застосунку через `dotnet run --project Schedule.Web`.
- Перевірити виконання тестів через `dotnet test Schedule.slnx`.
- Зафіксувати приклади відповідей Dekanat API для:
  - групи;
  - викладача;
  - аудиторії;
  - списку об’єктів;
  - suggestions для викладачів і груп.
- Скласти короткий список фактичних полів, які використовуються UI.
- Перевірити формати дат, номерів пар, типів занять і текстів замін.
- Зафіксувати поточні URL для iCal-експорту.

### Результат

З’являються тестові fixtures або snapshots зовнішніх відповідей. Це дозволить мігрувати backend без залежності кожного тесту від доступності зовнішнього сервісу.

---

## 6. Етап 1 — перебудувати solution та проєкти

### Завдання

- Створити `src/` і `tests/`.
- Перенести `Schedule.Web` у `src/Schedule.Web` без зміни поведінки на цьому етапі або залишити сумісний шлях до завершення міграції.
- Створити проєкти:
  - `Schedule.Api` — ASP.NET Core Web API;
  - `Schedule.Contracts` — Class Library;
  - `Schedule.Client.Core` — Class Library;
  - `Schedule.Client.UI` — Razor Class Library;
  - `Schedule.Mobile` — .NET MAUI Blazor Hybrid.
- Додати всі проєкти до `Schedule.sln` і `Schedule.slnx`.
- Встановити єдину цільову версію .NET, узгоджену з підтримкою встановленого SDK і MAUI.
- Перенести спільні build-настройки, nullable-настройки та аналізатори в один `.props`/`.targets` файл, якщо це буде потрібно.

### Результат

Solution збирається з порожніми або базовими host-проєктами, а залежності між проєктами відповідають цільовій схемі.

---

## 7. Етап 2 — спроєктувати `Schedule.Contracts`

### 7.1. Спільні enum-и

Створити власні enum-и, які не залежать від Dekanat SDK:

- `ScheduleTargetType`: `Teacher`, `Group`, `Room`;
- `LessonType`: `Lecture`, `Laboratory`, `Practical`, `Seminar`, `Credit`, `ExamConsultation`, `Exam`, `Other`;
- `HourType`: `Hourly`, `PartTime`, `FullTime`;
- `ScheduleViewMode`: `List`, `Week`.

Не переносити SDK enum-и напряму в contracts.

### 7.2. DTO розкладу

Запланувати DTO приблизно такого призначення:

- `ScheduleQueryRequest`:
  - тип об’єкта;
  - `ObjectId`;
  - `FromDate`;
  - `ToDate`;
  - режим відображення за потреби;
- `ScheduleResponse`:
  - назва об’єкта;
  - тип об’єкта;
  - період;
  - список занять;
  - необов’язкові grouped/week-представлення, якщо їх потрібно формувати на API;
- `LessonDto`:
  - дата;
  - номер і час пари;
  - назва заняття;
  - опис;
  - аудиторія;
  - викладач/група за наявності;
  - тип заняття;
  - тип годин;
  - кількість годин;
  - ознаки заміни та півпари.

### 7.3. Reference data

- `ReferenceDataResponse`;
- `BuildingDto`;
- `RoomDto`;
- `DepartmentDto`;
- `TeacherDto`;
- `GroupDto`;
- `ReferenceEntityDto` або окремі DTO для типобезпечності.

### 7.4. Розрахунки

- `TeachingLoadRequest` і `TeachingLoadResponse`;
- `TeachingLoadRowDto`;
- `AuditoriumLoadRequest` і `AuditoriumLoadResponse`;
- `AuditoriumLoadItemDto`;
- `WorkloadSessionTypeDto`, якщо типи занять потрібно віддавати структуровано.

### 7.5. Suggestions і помилки

- `SuggestionQuery`;
- `SuggestionItemDto`;
- єдиний формат помилки, наприклад `ApiErrorResponse`:
  - код;
  - повідомлення для користувача;
  - correlation/request id;
  - технічні деталі тільки в логах.

### Правила DTO

- Використовувати ISO-формат дат у JSON: `yyyy-MM-dd`.
- Не передавати `DateOnly` у форматі, який залежить від локалі сервера.
- Визначити nullable-поля явно.
- Додати JSON enum converter або зафіксувати строкові значення enum-ів.
- Не включати UI-специфічні властивості на кшталт Bootstrap-класів у contracts.

---

## 8. Етап 3 — винести backend у `Schedule.Api`

### 8.1. Перенесення існуючих сервісів

Із поточного `Schedule.Web` перенести та адаптувати:

| Поточний файл | Нове розташування | Зміна |
|---|---|---|
| `Services/ScheduleQueryService.cs` | `Schedule.Api/Services` | Інкапсулює виклики SDK і повертає власні DTO |
| `Services/ScheduleReferenceService.cs` | `Schedule.Api/Services` | Мапить SDK reference data у contracts |
| `Services/TimetableSuggestionsService.cs` | `Schedule.Api/Services` | Залишається серверним proxy до `timetable.cgi` |
| `Services/AuditoriumLoadService.cs` | `Schedule.Api/Services` | Переносить розрахунок у backend |
| частина `ScheduleDisplayService.cs` | `Schedule.Api/Services` або `Schedule.Client.Core` | Розділити API-розрахунки та UI-підготовку |
| `Services/FacultyConstants.cs` | `Schedule.Api/Configuration` або `Schedule.Contracts` | Визначити, чи це backend-довідник, чи публічний contract |
| `ViewModels/*` | `Schedule.Contracts` | Перетворити на request/response DTO |
| `Models/Lesson.cs` | `Schedule.Contracts` або API mapping | Не залишати SDK/legacy модель у UI |

`HomeController` не переносити як є. Його логіку розділити на API endpoint-и, валідатори та application services.

### 8.2. Рекомендовані endpoint-и

Базовий префікс: `/api/v1`.

#### Reference data

```text
GET /api/v1/reference
GET /api/v1/reference/buildings
GET /api/v1/reference/departments/teachers
GET /api/v1/reference/departments/groups
```

На першій ітерації достатньо одного `GET /api/v1/reference`, якщо обсяг даних прийнятний. Розділені endpoint-и залишити як варіант для оптимізації завантаження.

#### Розклад

```text
GET /api/v1/schedules/teachers/{teacherId}?from=2026-08-24&to=2026-08-30
GET /api/v1/schedules/groups/{groupId}?from=2026-08-24&to=2026-08-30
GET /api/v1/schedules/rooms/{roomId}?from=2026-08-24&to=2026-08-30
```

Альтернатива для одного універсального endpoint-а:

```text
GET /api/v1/schedules/{targetType}/{objectId}?from=...&to=...
```

Краще обрати один стиль і зафіксувати його в OpenAPI до початку клієнтської реалізації.

#### Suggestions

```text
GET /api/v1/suggestions/teachers?facultyId=...
GET /api/v1/suggestions/groups?facultyId=...&course=...
```

#### Розрахунки

```text
GET /api/v1/workload/teachers?department=...&from=...&to=...
GET /api/v1/workload/auditoriums?building=...&from=...&to=...&pairsFrom=1&pairsTo=6
```

Для складних запитів із багатьма параметрами допустимо використовувати `POST` з request DTO, але для read-only розрахунків бажано почати з `GET`.

#### iCal

Замість віддачі необробленого зовнішнього URL бажано мати контрольований endpoint:

```text
GET /api/v1/schedules/{targetType}/{objectId}/ical?from=...&to=...
```

API може проксувати iCal-відповідь від Dekanat або повернути безпечний URL, якщо це не створює проблем із CORS та доступом.

### 8.3. Валідація

Перевіряти на API:

- коректність типу об’єкта;
- непорожній `objectId`;
- `from <= to`;
- обмеження максимальної довжини періоду;
- коректність діапазону пар;
- допустимі значення `facultyId` і `course`;
- існування об’єкта в reference data, якщо це можливо перевірити без зайвого зовнішнього запиту.

Клієнтська валідація потрібна для UX, але не замінює серверну.

### 8.4. Обробка зовнішнього API

- Додати timeout для викликів Dekanat SDK і `timetable.cgi`.
- Використовувати cancellation token.
- Відокремити помилки зовнішнього сервісу від помилок валідації.
- Додати retry тільки для тимчасових помилок і з обмеженням кількості повторів.
- Не робити необмежений паралелізм запитів до аудиторій.
- Поточне обмеження `Concurrency = 4` залишити конфігурованим.
- Логувати помилки з object id, періодом і correlation id, але не витікати технічні деталі в UI.

### 8.5. Кешування reference data

Reference data змінюються рідше за розклад. Додати серверний кеш:

- `IMemoryCache` для однієї інстанції;
- distributed cache, якщо буде кілька інстанцій;
- TTL, наприклад 5–30 хвилин, підібрати після вимірювань;
- окремий ключ для reference data та suggestions;
- можливість ручного очищення кешу в development/diagnostic режимі.

Розклад можна кешувати обережніше, з коротким TTL, оскільки заміни можуть змінюватися протягом дня.

### 8.6. Документація API

- Увімкнути Swagger/OpenAPI у development і staging.
- Описати всі query-параметри, формати дат, коди помилок і приклади відповідей.
- Додати contract tests на сумісність DTO.
- Визначити політику версій: `/api/v1`.

---

## 9. Етап 4 — реалізувати `Schedule.Client.Core`

### 9.1. HTTP API client

Створити абстракції:

- `IScheduleApiClient`;
- `ScheduleApiClient`;
- `IReferenceDataClient` або об’єднати його з основним клієнтом;
- `IApiErrorHandler`/спільний mapper помилок.

Клієнт повинен:

- приймати `HttpClient` через DI;
- мати базову адресу з конфігурації host-застосунку;
- додавати cancellation token;
- десеріалізувати contracts;
- перетворювати HTTP-помилки у типізовані винятки або `Result`;
- не містити Blazor-компонентів;
- однаково працювати в браузері та MAUI.

### 9.2. Спільні application services

Залежно від обсягу логіки додати:

- `ScheduleService` — отримання і форматування розкладу;
- `ReferenceDataService` — завантаження довідників;
- `SuggestionsService` — пошук/фільтрація викладачів і груп;
- `TeachingLoadService` — виклик розрахунку навантаження;
- `AuditoriumLoadService` — виклик розрахунку аудиторій;
- `CalendarExportService` — формування URL або завантаження iCal.

Важливо розділити:

- backend-розрахунок, який має бути однаковим для всіх клієнтів;
- локальне форматування дати, часу та presentation state.

### 9.3. Стан клієнта

Створити простий state container без прив’язки до конкретної платформи:

- `ReferenceDataState`;
- `ScheduleQueryState`;
- `ScheduleResultState`;
- `AppErrorState`;
- `UserPreferencesState`.

Початково не потрібен складний Redux-подібний підхід. Достатньо scoped state service з подією `StateChanged` та явними методами оновлення.

### 9.4. Локальне зберігання

Зробити абстракцію:

```text
IUserPreferencesStorage
```

Web-реалізація:

- `localStorage` через JS interop або відповідний storage package.

Android-реалізація:

- `Preferences` або secure/local storage MAUI.

Зберігати тільки налаштування UX:

- останній обраний tab;
- остання група/викладач;
- стандартний період;
- режим list/week;
- тему, якщо вона буде.

Не зберігати токени або чутливі дані без окремої вимоги.

---

## 10. Етап 5 — створити спільний `Schedule.Client.UI`

### 10.1. Сторінки

Запланувати Razor-сторінки:

- `Pages/Today.razor` — заняття на сьогодні;
- `Pages/Week.razor` — тижневий вигляд;
- `Pages/Schedule.razor` — універсальний пошук;
- `Pages/Teachers.razor` — вибір і розклад викладача;
- `Pages/Groups.razor` — вибір і розклад групи;
- `Pages/Rooms.razor` — розклад аудиторії;
- `Pages/AuditoriumLoad.razor` — завантаження аудиторій;
- `Pages/TeachingLoad.razor` — навчальне навантаження;
- `Pages/Settings.razor` — локальні налаштування;
- `Pages/NotFound.razor`;
- `Pages/Error.razor`.

На першій ітерації можна залишити один `Schedule.razor` із вкладками, повторивши поточний UX, а потім розділити сторінки без зміни API.

### 10.2. Компоненти

Винести з поточних MVC Views такі компоненти:

- `ScheduleDay.razor`;
- `ScheduleWeek.razor`;
- `LessonCard.razor`;
- `LessonList.razor`;
- `ScheduleSearchForm.razor`;
- `PeriodSelector.razor`;
- `FacultySelector.razor`;
- `TeacherSelector.razor`;
- `GroupSelector.razor`;
- `RoomSelector.razor`;
- `BuildingSelector.razor`;
- `TeachingLoadTable.razor`;
- `AuditoriumLoadTable.razor`;
- `LoadingIndicator.razor`;
- `ErrorAlert.razor`;
- `EmptyScheduleState.razor`;
- `CalendarExportButton.razor`.

Компонент рівня дня має бути приблизно таким:

```razor
<ScheduleDay Date="@Date" Lessons="@Lessons" />
```

Він не повинен знати, чи був викликаний із Web або Android.

### 10.3. Layout і навігація

- `MainLayout.razor`;
- `NavMenu.razor`;
- `PageTitle.razor`;
- `ResponsiveContainer.razor`.

Web і Mobile можуть мати спільний основний layout, але дозволено мати platform-specific navigation wrapper, якщо Android потребуватиме native navigation pattern.

### 10.4. CSS

Перенести стилі з `Schedule.Web/wwwroot/css/site.css` у RCL:

- спільні компоненти — у `Schedule.Client.UI/wwwroot/css/schedule-ui.css`;
- platform-specific стилі — у відповідному host-проєкті;
- не покладатися на MVC partial views;
- перевірити responsive layout для вузького Android-екрана;
- зберегти доступність: keyboard navigation у Web, достатні touch targets в Android, aria-label для кнопок та селекторів.

Bootstrap можна залишити, але треба зафіксувати спосіб підключення так, щоб він працював і в Web, і в `BlazorWebView`.

---

## 11. Етап 6 — створити `Schedule.Web`

### Технологія

Використати `Blazor Web App` з інтерактивністю `Interactive WebAssembly`.

Рекомендована стратегія:

- SSR/статичний shell для початкового завантаження;
- інтерактивні сторінки через WebAssembly;
- HTTP-запити тільки до `Schedule.Api`;
- без прямого доступу до Dekanat SDK.

### Завдання

- Створити Web host на базі Blazor Web App.
- Додати посилання на `Schedule.Client.UI` і `Schedule.Client.Core`.
- Зареєструвати `HttpClient` із `ApiBaseUrl`.
- Підключити shared routes і layout.
- Додати конфігурацію API для development/staging/production.
- Додати `appsettings` або runtime configuration без захардкодженої production URL у shared library.
- Налаштувати CORS на API для web origin.
- Перенести favicon, manifest і web-specific meta tags.
- Перенести логіку вибору активної вкладки зі старого MVC UI у router/state.

### SSR/WASM рішення

Визначити для кожної сторінки режим render mode:

- інформаційний shell може бути SSR;
- сторінки з вибором групи, викладача, дат і запитами — interactive WebAssembly;
- не вмикати глобальний режим, якщо це збільшує payload без користі.

### Web-specific можливості

- `localStorage` для налаштувань;
- browser download для iCal;
- service worker/PWA — окремий етап після базового запуску;
- web analytics — тільки після погодження вимог і privacy-політики.

---

## 12. Етап 7 — створити `Schedule.Mobile` для Android

### Технологія

Використати `.NET MAUI Blazor Hybrid` із `BlazorWebView`.

Shared Razor UI запускається всередині native .NET runtime на пристрої. Android host не дублює сторінки, а надає platform-specific оболонку та native services.

### Завдання

- Створити MAUI Blazor Hybrid-проєкт.
- Додати посилання на `Schedule.Client.UI`, `Schedule.Client.Core` і `Schedule.Contracts`.
- Налаштувати `MauiProgram.cs`:
  - DI для shared services;
  - `HttpClient` із mobile API base URL;
  - storage implementation;
  - logging;
  - optional connectivity service.
- Створити `MainPage.xaml` з `BlazorWebView`.
- Додати Android manifest, package id, app icon і splash screen.
- Підтримати portrait layout як мінімум; landscape — за потреби.
- Перевірити back button, app resume та втрату/відновлення мережі.

### Android-specific services

Підготувати абстракції, але реалізувати тільки необхідне:

- `IConnectivityService`;
- `INotificationService`;
- `IUserPreferencesStorage`;
- `ICalendarExportService`;
- `IAppLifecycleService`.

Push/local notifications не включати в першу міграцію, якщо немає чіткої вимоги. Спочатку достатньо native shell та перегляду розкладу.

### Offline-режим

Запланувати після MVP:

- локальний кеш останнього успішного розкладу;
- timestamp останнього оновлення;
- повідомлення “дані можуть бути неактуальними”;
- черга повторного завантаження після відновлення мережі.

Не робити повноцінну offline-синхронізацію до того, як буде стабільний API-контракт.

---

## 13. Етап 8 — перехідний період і видалення MVC

### Рекомендований порядок

1. Новий API повертає ті самі дані, що й поточний MVC UI.
2. Shared UI відтворює основний сценарій пошуку розкладу.
3. `Schedule.Web` переходить на Blazor і викликає API.
4. `Schedule.Mobile` підключає ті самі сторінки.
5. Порівняти старий MVC і новий Web за контрольними сценаріями.
6. Видалити MVC controllers, views та їхні специфічні ViewModels.
7. Видалити прямі залежності від `Dekanat.ScheduleSdk` з клієнтських проєктів.

### Що не треба робити

- Не залишати два незалежні backend-и з різними правилами розрахунку.
- Не давати Web/Android прямий доступ до зовнішнього Dekanat API.
- Не переносити `HomeController` у shared code.
- Не копіювати MVC partial views у Razor без розділення даних, стану й UI.
- Не змішувати DTO API, SDK-моделі та UI ViewModel в одному namespace.

---

## 14. Тестування

### Unit-тести API

Покрити:

- парсинг і нормалізацію дат;
- визначення типу заняття;
- визначення типу годин;
- півпари та заміни;
- групування занять за датами;
- побудову тижневого вигляду;
- розрахунок робочих днів;
- розрахунок навчального навантаження;
- розрахунок завантаження аудиторій;
- валідацію періодів і діапазону пар.

Існуючі перевірки з `LessonTests.cs` і `PeriodOptionsProviderTests.cs` перенести до відповідних тестових проєктів та розширити для нових DTO/application services.

### Integration-тести API

- використати `WebApplicationFactory`;
- замінити Dekanat client на fake/mock adapter;
- перевірити HTTP status codes;
- перевірити JSON shape і формат дат;
- перевірити помилки зовнішнього сервісу;
- перевірити cancellation/timeout поведінку, де це можливо.

### Contract-тести

- згенерувати або перевірити OpenAPI;
- зафіксувати приклади JSON-відповідей;
- перевіряти, що Web і Mobile коректно десеріалізують API contracts.

### UI-тести

- перевірити відображення порожнього розкладу;
- перевірити list/week view;
- перевірити стани loading/error/success;
- перевірити, що вибір факультету оновлює групи та викладачів;
- перевірити адаптивність основних компонентів у браузері.

### Android smoke tests

- запуск застосунку;
- відкриття головної сторінки;
- завантаження reference data;
- пошук розкладу групи;
- back navigation;
- поведінка при відсутній мережі.

---

## 15. Конфігурація середовищ

### API

```text
Schedule.Api
├─ appsettings.json
├─ appsettings.Development.json
├─ appsettings.Staging.json
└─ appsettings.Production.json
```

Винести в конфігурацію:

- базову адресу Dekanat API;
- адресу `timetable.cgi`;
- timeout-и;
- retry policy;
- cache TTL;
- максимальний період запиту;
- максимальний паралелізм аудиторій;
- дозволені CORS origins.

### Web

Винести в конфігурацію:

- `ApiBaseUrl`;
- environment name;
- feature flags;
- optional PWA settings.

### Android

Винести в конфігурацію:

- production/staging API URL;
- application version;
- telemetry flags, якщо вони з’являться.

Не зберігати production secrets у git або в APK.

---

## 16. Безпека та надійність

- Додати CORS тільки для відомих Web origin-ів.
- Не дозволяти API приймати довільні зовнішні URL від клієнта.
- Валідовувати всі `objectId` і query-параметри.
- Додати rate limiting для дорогих endpoint-ів, особливо auditorium load.
- Обмежити максимальний діапазон дат.
- Додати structured logging і correlation id.
- Не показувати stack trace або URL внутрішніх сервісів користувачу.
- У production увімкнути HTTPS.
- Для API health checks додати `/health` і readiness перевірку зовнішньої залежності за потреби.
- Визначити політику щодо кешованих даних і privacy до публічного запуску.

---

## 17. CI/CD і deployment

### CI

У `.github/workflows` додати pipeline, який:

1. відновлює залежності;
2. збирає solution;
3. запускає unit та integration tests;
4. перевіряє формат і warnings;
5. генерує OpenAPI artifact;
6. збирає Web;
7. за наявності Android workload збирає MAUI Android package.

### Deployment targets

Окремо підготувати:

- `Schedule.Api` — контейнер або ASP.NET hosting;
- `Schedule.Web` — static/ASP.NET deployment залежно від обраного режиму Blazor Web App;
- `Schedule.Mobile` — signed APK/AAB для Android.

Рекомендовані адреси:

```text
https://api.schedule.nung.edu.ua
https://schedule.nung.edu.ua
```

Для Android production build використовувати тільки HTTPS API endpoint.

### Версіювання

- API: `/api/v1`;
- Web і Mobile: показувати app/client version у diagnostic information;
- зміни contracts робити backward-compatible або підвищувати версію API.

---

## 18. Пропонований порядок реалізації MVP

Щоб зменшити ризик, першу робочу версію слід будувати вертикальними зрізами:

### MVP-1: API + базовий Web

- `Schedule.Contracts`;
- `Schedule.Api`;
- `GET /api/v1/reference`;
- розклад групи/викладача/аудиторії;
- list view;
- базова обробка помилок;
- Blazor Web App із shared UI.

### MVP-2: повний shared UI

- week view;
- suggestions;
- iCal;
- teaching load;
- auditorium load;
- адаптивний дизайн.

### MVP-3: Android

- MAUI Blazor Hybrid host;
- підключення shared UI;
- Android storage;
- back navigation;
- network error state;
- signed debug/release build.

### MVP-4: production hardening

- кешування;
- rate limiting;
- telemetry/logging;
- health checks;
- CI/CD;
- performance testing;
- optional offline cache і notifications.

---

## 19. Критерії готовності

Міграцію можна вважати завершеною, коли:

- solution збирається однією командою;
- API не залежить від MVC Views і не повертає SDK-моделі;
- Web і Android використовують один `Schedule.Client.UI`;
- Web і Android використовують один `Schedule.Client.Core`;
- жоден клієнт не звертається напряму до Dekanat API;
- група, викладач і аудиторія мають однаковий результат у Web і Android;
- працюють list/week view;
- працюють iCal, teaching load та auditorium load;
- помилки зовнішнього сервісу коректно відображаються користувачу;
- є unit, integration і базові UI/smoke-тести;
- API має OpenAPI-документацію;
- Web production build працює через HTTPS;
- Android release build встановлюється та підключається до production API;
- старі MVC controllers, views і непотрібні ViewModels видалені після завершення перехідного періоду.

---

## 20. Перші конкретні зміни у репозиторії

Рекомендований безпечний порядок комітів:

1. `chore: add src and tests solution folders` — структура solution без зміни runtime.
2. `feat: add schedule contracts` — DTO, enum-и та помилки API.
3. `refactor: extract schedule backend services` — перенесення SDK-інтеграції у `Schedule.Api`.
4. `feat: expose schedule api v1` — endpoint-и, валідація, Swagger і integration tests.
5. `feat: add client core` — typed API client, state і storage abstractions.
6. `feat: add shared razor ui` — сторінки та компоненти в RCL.
7. `feat: migrate web to blazor` — Web host переходить з MVC на Blazor Web App.
8. `feat: add maui android client` — MAUI Blazor Hybrid host.
9. `test: add cross-client and api coverage` — contract, UI та smoke-тести.
10. `chore: remove legacy mvc` — видалення старого MVC-коду після порівняння результатів.

На кожному кроці бажано залишати solution у стані, який збирається і проходить тести.
