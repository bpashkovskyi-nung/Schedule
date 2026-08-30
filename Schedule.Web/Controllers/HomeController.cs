using Dekanat.ScheduleSdk.Enums;
using Dekanat.ScheduleSdk.Exceptions;
using Dekanat.ScheduleSdk.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Schedule.Web.Configuration;
using Schedule.Web.Services;
using Schedule.Web.ViewModels;

namespace Schedule.Web.Controllers;

public sealed class HomeController(
    IScheduleReferenceService referenceService,
    IScheduleQueryService scheduleQuery,
    ITimetableSuggestionsService suggestionsService,
    IAuditoriumLoadService auditoriumLoadService,
    PeriodOptionsProvider periodOptions,
    IOptions<LegacyFeatureOptions> featureOptions,
    ILegacyIcalGenerator icalGenerator) : Controller
{
    [HttpGet]
    public IActionResult Error() => View();

    [HttpGet]
    public async Task<IActionResult> Index(string? tab, CancellationToken cancellationToken)
    {
        ScheduleIndexViewModel model = await BuildBaseModelAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(tab))
        {
            model.ActiveTab = tab;
        }

        ApplyDefaultDates(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Unified(UnifiedScheduleFormVm form, CancellationToken cancellationToken) =>
        SearchUnifiedAsync(form, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Rooms(RoomsScheduleFormVm form, CancellationToken cancellationToken) =>
        SearchRoomsAsync(form, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Teachers(TeachersScheduleFormVm form, CancellationToken cancellationToken) =>
        SearchTeachersAsync(form, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Groups(GroupsScheduleFormVm form, CancellationToken cancellationToken) =>
        SearchGroupsAsync(form, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> TeachingLoad(TeachingLoadFormVm form, CancellationToken cancellationToken) =>
        SearchTeachingLoadAsync(form, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AuditoriumLoad(AuditoriumLoadFormVm form, CancellationToken cancellationToken) =>
        SearchAuditoriumLoadAsync(form, cancellationToken);

    [HttpGet]
    public async Task<IActionResult> ReloadUnifiedLists(
        string? facultyName,
        int course,
        CancellationToken cancellationToken)
    {
        ScheduleIndexViewModel model = await BuildBaseModelAsync(cancellationToken);
        model.ActiveTab = "main-schedule";
        model.Unified.FacultyName = facultyName;
        model.Unified.Course = course;
        await PopulateUnifiedListsAsync(model, cancellationToken);
        ApplyDefaultDates(model);
        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Ical(
        string? mode,
        string? objectId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (!featureOptions.Value.GenerateIcalFromJson)
        {
            return NotFound();
        }

        if (!TryParseScheduleMode(mode, out RequestMode parsedMode) ||
            string.IsNullOrWhiteSpace(objectId) ||
            (from is null) != (to is null))
        {
            return BadRequest("Некоректні параметри iCal-запиту.");
        }

        string normalizedObjectId = objectId.Trim();
        (DateOnly fromDate, DateOnly toDate) = ResolveIcalDates(from, to);

        if (fromDate > toDate)
        {
            return BadRequest("Некоректний діапазон дат iCal-запиту.");
        }

        try
        {
            PsRozkladExport export = await scheduleQuery.GetScheduleAsync(
                parsedMode,
                normalizedObjectId,
                fromDate,
                toDate,
                cancellationToken);

            byte[] calendar = icalGenerator.Generate(export, normalizedObjectId);
            string fileName = $"{SanitizeFileName(normalizedObjectId)}.ics";
            return File(calendar, "text/calendar; charset=utf-8", fileName);
        }
        catch (PsRozkladApiException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, exception.ErrorMessage);
        }
    }

    private async Task<IActionResult> SearchUnifiedAsync(
        UnifiedScheduleFormVm form,
        CancellationToken cancellationToken)
    {
        ScheduleIndexViewModel model = await BuildBaseModelAsync(cancellationToken);
        model.ActiveTab = "main-schedule";
        model.Unified = form;

        try
        {
            await PopulateUnifiedListsAsync(model, cancellationToken);

            if (string.IsNullOrWhiteSpace(form.FacultyName))
            {
                model.ErrorMessage = "Оберіть факультет та період дат";
                return View("Index", model);
            }

            if (string.IsNullOrEmpty(form.TeacherId) && string.IsNullOrEmpty(form.GroupId))
            {
                model.ErrorMessage = "Оберіть викладача або групу";
                return View("Index", model);
            }

            if (!string.IsNullOrEmpty(form.TeacherId) && !string.IsNullOrEmpty(form.GroupId))
            {
                model.ErrorMessage = "Оберіть лише викладача або групу, не обидва поля";
                return View("Index", model);
            }

            (DateOnly from, DateOnly to) = PeriodOptionsProvider.ResolveDates(
                form.PeriodKey, form.FromDate, form.ToDate, model.PeriodOptions);

            bool isTeacher = !string.IsNullOrEmpty(form.TeacherId);
            RequestMode mode = isTeacher ? RequestMode.Teacher : RequestMode.Group;
            string objectId = isTeacher ? form.TeacherId! : form.GroupId!;
            PsRozkladExport export = await scheduleQuery.GetScheduleAsync(mode, objectId, from, to, cancellationToken);

            string title = isTeacher
                ? ScheduleLookup.FindTeacherName(model.Reference, objectId) ?? "Викладач"
                : ScheduleLookup.FindGroupName(model.Reference, objectId) ?? "Група";

            model.Unified.Result = BuildScheduleResult(export, title, form.WeeklyView, mode, objectId, from, to, scheduleQuery);
        }
        catch (PsRozkladApiException ex)
        {
            model.ErrorMessage = ex.ErrorMessage;
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
        }

        ApplyDefaultDates(model);
        return View("Index", model);
    }

    private async Task<IActionResult> SearchRoomsAsync(
        RoomsScheduleFormVm form,
        CancellationToken cancellationToken)
    {
        ScheduleIndexViewModel model = await BuildBaseModelAsync(cancellationToken);
        model.ActiveTab = "rooms";
        model.Rooms = form;

        try
        {
            if (string.IsNullOrEmpty(form.RoomId))
            {
                model.ErrorMessage = "Необхідно заповнити всі поля";
                return View("Index", model);
            }

            (DateOnly from, DateOnly to) = PeriodOptionsProvider.ResolveDates(
                form.PeriodKey, form.FromDate, form.ToDate, model.PeriodOptions);

            PsRozkladExport export = await scheduleQuery.GetScheduleAsync(
                RequestMode.Room, form.RoomId, from, to, cancellationToken);

            string title = ScheduleLookup.FindRoomName(model.Reference, form.RoomId) ?? "Аудиторія";
            model.Rooms.Result = BuildScheduleResult(
                export, title, form.WeeklyView, RequestMode.Room, form.RoomId, from, to, scheduleQuery);
        }
        catch (PsRozkladApiException ex)
        {
            model.ErrorMessage = ex.ErrorMessage;
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
        }

        ApplyDefaultDates(model);
        return View("Index", model);
    }

    private async Task<IActionResult> SearchTeachersAsync(
        TeachersScheduleFormVm form,
        CancellationToken cancellationToken)
    {
        ScheduleIndexViewModel model = await BuildBaseModelAsync(cancellationToken);
        model.ActiveTab = "teachers";
        model.Teachers = form;

        try
        {
            if (string.IsNullOrEmpty(form.TeacherId))
            {
                model.ErrorMessage = "Необхідно заповнити всі поля";
                return View("Index", model);
            }

            (DateOnly from, DateOnly to) = PeriodOptionsProvider.ResolveDates(
                form.PeriodKey, form.FromDate, form.ToDate, model.PeriodOptions);

            PsRozkladExport export = await scheduleQuery.GetScheduleAsync(
                RequestMode.Teacher, form.TeacherId, from, to, cancellationToken);

            string title = ScheduleLookup.FindTeacherName(model.Reference, form.TeacherId) ?? "Викладач";
            model.Teachers.Result = BuildScheduleResult(
                export, title, form.WeeklyView, RequestMode.Teacher, form.TeacherId, from, to, scheduleQuery);
        }
        catch (PsRozkladApiException ex)
        {
            model.ErrorMessage = ex.ErrorMessage;
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
        }

        ApplyDefaultDates(model);
        return View("Index", model);
    }

    private async Task<IActionResult> SearchGroupsAsync(
        GroupsScheduleFormVm form,
        CancellationToken cancellationToken)
    {
        ScheduleIndexViewModel model = await BuildBaseModelAsync(cancellationToken);
        model.ActiveTab = "groups";
        model.Groups = form;

        try
        {
            if (string.IsNullOrEmpty(form.GroupId))
            {
                model.ErrorMessage = "Необхідно заповнити всі поля";
                return View("Index", model);
            }

            (DateOnly from, DateOnly to) = PeriodOptionsProvider.ResolveDates(
                form.PeriodKey, form.FromDate, form.ToDate, model.PeriodOptions);

            PsRozkladExport export = await scheduleQuery.GetScheduleAsync(
                RequestMode.Group, form.GroupId, from, to, cancellationToken);

            string title = ScheduleLookup.FindGroupName(model.Reference, form.GroupId) ?? "Група";
            model.Groups.Result = BuildScheduleResult(
                export, title, form.WeeklyView, RequestMode.Group, form.GroupId, from, to, scheduleQuery);
        }
        catch (PsRozkladApiException ex)
        {
            model.ErrorMessage = ex.ErrorMessage;
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
        }

        ApplyDefaultDates(model);
        return View("Index", model);
    }

    private async Task<IActionResult> SearchTeachingLoadAsync(
        TeachingLoadFormVm form,
        CancellationToken cancellationToken)
    {
        ScheduleIndexViewModel model = await BuildBaseModelAsync(cancellationToken);
        model.ActiveTab = "teaching-load";
        model.TeachingLoad = form;

        try
        {
            if (string.IsNullOrWhiteSpace(form.DepartmentName))
            {
                model.ErrorMessage = "Необхідно заповнити всі поля";
                return View("Index", model);
            }

            (DateOnly from, DateOnly to) = PeriodOptionsProvider.ResolveDates(
                form.PeriodKey, form.FromDate, form.ToDate, model.PeriodOptions);

            IReadOnlyList<ScheduleEntityVm> teachers =
                ScheduleLookup.GetTeachersForDepartment(model.Reference, form.DepartmentName);

            if (teachers.Count == 0)
            {
                model.ErrorMessage = "Підрозділ не знайдено";
                return View("Index", model);
            }

            List<PsRozkladExport> schedules = new(teachers.Count);
            foreach (ScheduleEntityVm teacher in teachers)
            {
                PsRozkladExport export = await scheduleQuery.GetScheduleAsync(
                    RequestMode.Teacher, teacher.Id, from, to, cancellationToken);
                schedules.Add(export);
            }

            model.TeachingLoad.Result = ScheduleDisplayService.BuildTeachingLoad(teachers, schedules);
        }
        catch (PsRozkladApiException ex)
        {
            model.ErrorMessage = ex.ErrorMessage;
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
        }

        ApplyDefaultDates(model);
        return View("Index", model);
    }

    private async Task<IActionResult> SearchAuditoriumLoadAsync(
        AuditoriumLoadFormVm form,
        CancellationToken cancellationToken)
    {
        ScheduleIndexViewModel model = await BuildBaseModelAsync(cancellationToken);
        model.ActiveTab = "auditorium-load";
        model.AuditoriumLoad = form;

        try
        {
            (DateOnly from, DateOnly to) = PeriodOptionsProvider.ResolveDates(
                form.PeriodKey, form.FromDate, form.ToDate, model.PeriodOptions);

            if (form.PairsFrom > form.PairsTo || form.PairsFrom < 1 || form.PairsTo < 1)
            {
                model.ErrorMessage = "Діапазон пар некоректний";
                return View("Index", model);
            }

            int workingDays = ScheduleDisplayService.CountWorkingDays(from, to);
            if (workingDays <= 0)
            {
                model.ErrorMessage = "На обраний період немає робочих днів (пн-пт)";
                return View("Index", model);
            }

            string scope = string.IsNullOrWhiteSpace(form.BlockName)
                ? "Усі корпуси."
                : $"Корпус: {form.BlockName}.";

            AuditoriumLoadResultVm result = await auditoriumLoadService.CalculateAsync(
                model.Reference,
                form.BlockName,
                from,
                to,
                form.PairsFrom,
                form.PairsTo,
                cancellationToken);

            model.AuditoriumLoad.Result = result;
            model.AuditoriumLoad.Summary =
                $"{scope} Робочих днів: {result.WorkingDays}. Пари: {form.PairsFrom}-{form.PairsTo}. " +
                $"Можливо пар у кожній аудиторії: {result.PossiblePairsPerRoom}.";

            if (result.ErrorCount > 0)
            {
                model.ErrorMessage =
                    $"Не вдалося завантажити розклад для {result.ErrorCount} аудиторій. Вони показані як 0%.";
            }
        }
        catch (PsRozkladApiException ex)
        {
            model.ErrorMessage = ex.ErrorMessage;
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
        }

        ApplyDefaultDates(model);
        return View("Index", model);
    }

    private async Task<ScheduleIndexViewModel> BuildBaseModelAsync(CancellationToken cancellationToken)
    {
        ReferenceDataVm reference = await referenceService.LoadReferenceDataAsync(cancellationToken);
        return new ScheduleIndexViewModel
        {
            PeriodOptions = periodOptions.GetPeriodOptions(),
            Reference = reference,
        };
    }

    private async Task PopulateUnifiedListsAsync(ScheduleIndexViewModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Unified.FacultyName))
        {
            model.Unified.AvailableTeachers = [];
            model.Unified.AvailableGroups = [];
            return;
        }

        int facultyId = FacultyConstants.GetFacultyId(model.Unified.FacultyName);
        if (facultyId <= 0)
        {
            return;
        }

        Dictionary<string, ScheduleEntityVm> teacherByFullName = model.Reference.TeacherDepartments
            .SelectMany(d => d.Entities)
            .Where(t => !string.IsNullOrWhiteSpace(t.FullName))
            .GroupBy(t => t.FullName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        IReadOnlyList<string> teacherSuggestions = await suggestionsService.GetSuggestionsAsync(
            141, facultyId, 0, cancellationToken: cancellationToken);

        model.Unified.AvailableTeachers = teacherSuggestions
            .Where(name => teacherByFullName.ContainsKey(name))
            .Select(name => teacherByFullName[name])
            .ToList();

        DepartmentVm? faculty = model.Reference.GroupDepartments
            .FirstOrDefault(d => FacultyConstants.NormalizeFacultyName(d.Name) ==
                                 FacultyConstants.NormalizeFacultyName(model.Unified.FacultyName!));

        if (faculty is null)
        {
            model.Unified.AvailableGroups = [];
            return;
        }

        IReadOnlyList<string> groupSuggestions = await suggestionsService.GetSuggestionsAsync(
            142, facultyId, model.Unified.Course, cancellationToken: cancellationToken);

        HashSet<string> allowed = groupSuggestions.ToHashSet(StringComparer.Ordinal);
        model.Unified.AvailableGroups = faculty.Entities
            .Where(g => model.Unified.Course <= 0 || allowed.Count == 0 || allowed.Contains(g.Name))
            .ToList();
    }

    private static ScheduleResultVm BuildScheduleResult(
        PsRozkladExport export,
        string title,
        bool weeklyView,
        RequestMode mode,
        string objectId,
        DateOnly from,
        DateOnly to,
        IScheduleQueryService queryService)
    {
        IReadOnlyList<ScheduleItem>? items = export.ScheduleItems ?? [];
        IReadOnlyDictionary<string, IReadOnlyList<ScheduleItemVm>> grouped =
            ScheduleDisplayService.GroupByDate(items);

        return new ScheduleResultVm
        {
            Title = title,
            IsWeeklyView = weeklyView,
            ExportUrl = weeklyView ? null : queryService.BuildExportUrl(mode, objectId, from, to),
            GroupedByDate = weeklyView ? null : grouped,
            Weeks = weeklyView ? ScheduleDisplayService.CreateWeeklyView(grouped) : null,
        };
    }

    private static void ApplyDefaultDates(ScheduleIndexViewModel model)
    {
        void Apply(DateRangeFormVm form)
        {
            PeriodOptionVm? option = model.PeriodOptions.FirstOrDefault(o => o.Key == form.PeriodKey)
                                   ?? model.PeriodOptions.First(o => o.Key == "to_end_of_week");

            if (form.PeriodKey != "custom" || form.FromDate is null)
            {
                form.FromDate = option.FromDate;
            }

            if (form.PeriodKey != "custom" || form.ToDate is null)
            {
                form.ToDate = option.ToDate;
            }
        }

        Apply(model.Unified);
        Apply(model.Rooms);
        Apply(model.Teachers);
        Apply(model.Groups);
        Apply(model.TeachingLoad);
        Apply(model.AuditoriumLoad);
    }

    private (DateOnly From, DateOnly To) ResolveIcalDates(DateOnly? from, DateOnly? to)
    {
        if (from is not null && to is not null)
        {
            return (from.Value, to.Value);
        }

        PeriodOptionVm currentTerm = periodOptions.GetPeriodOptions()
            .First(option => option.Key == "current_term");
        return (currentTerm.FromDate, currentTerm.ToDate);
    }

    private static bool TryParseScheduleMode(string? value, out RequestMode mode)
    {
        mode = value?.Trim().ToLowerInvariant() switch
        {
            "teacher" or "teachers" => RequestMode.Teacher,
            "group" or "groups" => RequestMode.Group,
            "room" or "rooms" or "auditorium" or "auditoriums" => RequestMode.Room,
            _ => default,
        };

        return value?.Trim().ToLowerInvariant() switch
        {
            "teacher" or "teachers" or "group" or "groups" or
            "room" or "rooms" or "auditorium" or "auditoriums" => true,
            _ => false,
        };
    }

    private static string SanitizeFileName(string value)
    {
        string fileName = string.Concat(value.Where(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' or ' '));
        fileName = fileName.Trim().Replace(' ', '_');

        if (fileName.Length > 80)
        {
            fileName = fileName[..80];
        }

        return string.IsNullOrWhiteSpace(fileName) ? "schedule" : fileName;
    }
}
