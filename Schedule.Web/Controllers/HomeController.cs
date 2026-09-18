using Microsoft.AspNetCore.Mvc;
using Schedule.Client.Core.Api;
using Schedule.Contracts.AuditoriumLoad;
using Schedule.Contracts.Schedules;
using Schedule.Contracts.Suggestions;
using Schedule.Contracts.Workload;
using Schedule.Web.Services;
using Schedule.Web.ViewModels;

namespace Schedule.Web.Controllers;

public sealed class HomeController(
    IScheduleReferenceService referenceService,
    IScheduleQueryService scheduleQuery,
    PeriodOptionsProvider periodOptions) : Controller
{
    [HttpGet]
    public IActionResult Error() => View();

    [HttpGet]
    public IActionResult Privacy() => View();

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

    [HttpGet]
    public Task<IActionResult> Unified(UnifiedScheduleFormVm form, CancellationToken cancellationToken) =>
        SearchUnifiedAsync(form, cancellationToken);

    [HttpGet]
    public Task<IActionResult> Rooms(RoomsScheduleFormVm form, CancellationToken cancellationToken) =>
        SearchRoomsAsync(form, cancellationToken);

    [HttpGet]
    public Task<IActionResult> Teachers(TeachersScheduleFormVm form, CancellationToken cancellationToken) =>
        SearchTeachersAsync(form, cancellationToken);

    [HttpGet]
    public Task<IActionResult> Groups(GroupsScheduleFormVm form, CancellationToken cancellationToken) =>
        SearchGroupsAsync(form, cancellationToken);

    [HttpGet]
    public Task<IActionResult> TeachingLoad(TeachingLoadFormVm form, CancellationToken cancellationToken) =>
        SearchTeachingLoadAsync(form, cancellationToken);

    [HttpGet]
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
    public IActionResult Ical(
        string? mode,
        string? objectId,
        DateOnly? from,
        DateOnly? to)
    {
        if (!TryParseScheduleMode(mode, out ScheduleTargetType targetType) ||
            string.IsNullOrWhiteSpace(objectId) ||
            (from is null) != (to is null))
        {
            return BadRequest("Некоректні параметри iCal-запиту.");
        }

        DateOnly beginDate = from ?? DateOnly.FromDateTime(DateTime.Today);
        DateOnly endDate = to ?? beginDate.AddDays(6);
        if (beginDate > endDate)
        {
            return BadRequest("Некоректний діапазон дат iCal-запиту.");
        }

        return Redirect(scheduleQuery.BuildExportUrl(targetType, objectId.Trim(), beginDate, endDate));
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
            ScheduleTargetType targetType = isTeacher ? ScheduleTargetType.Teacher : ScheduleTargetType.Group;
            string objectId = isTeacher ? form.TeacherId! : form.GroupId!;
            ScheduleResponse response = await scheduleQuery.GetScheduleAsync(
                targetType, objectId, from, to, cancellationToken);

            string title = isTeacher
                ? ScheduleLookup.FindTeacherName(model.Reference, objectId) ?? "Викладач"
                : ScheduleLookup.FindGroupName(model.Reference, objectId) ?? "Група";

            model.Unified.Result = BuildScheduleResult(
                response, title, form.WeeklyView, targetType, objectId, from, to);
        }
        catch (ScheduleApiException exception)
        {
            model.ErrorMessage = exception.Message;
        }
        catch (Exception exception)
        {
            model.ErrorMessage = exception.Message;
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
            ScheduleResponse response = await scheduleQuery.GetScheduleAsync(
                ScheduleTargetType.Room, form.RoomId, from, to, cancellationToken);
            string title = ScheduleLookup.FindRoomName(model.Reference, form.RoomId) ?? "Аудиторія";

            model.Rooms.Result = BuildScheduleResult(
                response, title, form.WeeklyView, ScheduleTargetType.Room, form.RoomId, from, to);
        }
        catch (Exception exception)
        {
            model.ErrorMessage = exception.Message;
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
            ScheduleResponse response = await scheduleQuery.GetScheduleAsync(
                ScheduleTargetType.Teacher, form.TeacherId, from, to, cancellationToken);
            string title = ScheduleLookup.FindTeacherName(model.Reference, form.TeacherId) ?? "Викладач";

            model.Teachers.Result = BuildScheduleResult(
                response, title, form.WeeklyView, ScheduleTargetType.Teacher, form.TeacherId, from, to);
        }
        catch (Exception exception)
        {
            model.ErrorMessage = exception.Message;
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
            ScheduleResponse response = await scheduleQuery.GetScheduleAsync(
                ScheduleTargetType.Group, form.GroupId, from, to, cancellationToken);
            string title = ScheduleLookup.FindGroupName(model.Reference, form.GroupId) ?? "Група";

            model.Groups.Result = BuildScheduleResult(
                response, title, form.WeeklyView, ScheduleTargetType.Group, form.GroupId, from, to);
        }
        catch (Exception exception)
        {
            model.ErrorMessage = exception.Message;
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
            TeachingLoadResponse result = await scheduleQuery.GetTeachingLoadAsync(
                form.DepartmentName, from, to, cancellationToken);

            model.TeachingLoad.Result = new TeachingLoadResultVm
            {
                SessionTypes = result.SessionTypes,
                Rows = result.Rows.Select(row => new TeachingLoadRowVm
                {
                    TeacherName = row.TeacherName,
                    HoursByType = row.HoursByType,
                    TotalHours = row.TotalHours,
                }).ToList(),
            };
        }
        catch (Exception exception)
        {
            model.ErrorMessage = exception.Message;
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

            AuditoriumLoadResponse result = await scheduleQuery.GetAuditoriumLoadAsync(
                form.BlockName, from, to, form.PairsFrom, form.PairsTo, cancellationToken);

            model.AuditoriumLoad.Result = new AuditoriumLoadResultVm
            {
                Items = result.Items.Select(item => new AuditoriumLoadItemVm
                {
                    RoomName = item.RoomName,
                    BlockName = item.BuildingName,
                    Percent = item.Percent,
                    ActualPairs = item.ActualPairs,
                    PossiblePairs = item.PossiblePairs,
                }).ToList(),
                ErrorCount = result.ErrorCount,
                WorkingDays = result.WorkingDays,
                PossiblePairsPerRoom = result.PossiblePairsPerRoom,
            };

            string scope = string.IsNullOrWhiteSpace(form.BlockName)
                ? "Усі корпуси."
                : $"Корпус: {form.BlockName}.";
            model.AuditoriumLoad.Summary =
                $"{scope} Робочих днів: {result.WorkingDays}. Пари: {form.PairsFrom}-{form.PairsTo}. " +
                $"Можливо пар у кожній аудиторії: {result.PossiblePairsPerRoom}.";

            if (result.ErrorCount > 0)
            {
                model.ErrorMessage =
                    $"Не вдалося завантажити розклад для {result.ErrorCount} аудиторій. Вони показані як 0%.";
            }
        }
        catch (Exception exception)
        {
            model.ErrorMessage = exception.Message;
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

    private async Task PopulateUnifiedListsAsync(
        ScheduleIndexViewModel model,
        CancellationToken cancellationToken)
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

        SuggestionsResponse teacherSuggestions = await scheduleQuery.GetTeacherSuggestionsAsync(
            facultyId, cancellationToken);
        HashSet<string> teacherNames = teacherSuggestions.Suggestions.ToHashSet(StringComparer.Ordinal);
        model.Unified.AvailableTeachers = model.Reference.TeacherDepartments
            .SelectMany(department => department.Entities)
            .Where(teacher => teacherNames.Contains(teacher.FullName))
            .OrderBy(teacher => teacher.FullName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        DepartmentVm? faculty = model.Reference.GroupDepartments
            .FirstOrDefault(department => FacultyConstants.NormalizeFacultyName(department.Name) ==
                                          FacultyConstants.NormalizeFacultyName(model.Unified.FacultyName));
        if (faculty is null)
        {
            model.Unified.AvailableGroups = [];
            return;
        }

        SuggestionsResponse groupSuggestions = await scheduleQuery.GetGroupSuggestionsAsync(
            facultyId, model.Unified.Course, cancellationToken);
        HashSet<string> groupNames = groupSuggestions.Suggestions.ToHashSet(StringComparer.Ordinal);
        model.Unified.AvailableGroups = faculty.Entities
            .Where(group => model.Unified.Course <= 0 || groupNames.Count == 0 || groupNames.Contains(group.Name))
            .OrderBy(group => group.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private ScheduleResultVm BuildScheduleResult(
        ScheduleResponse response,
        string title,
        bool weeklyView,
        ScheduleTargetType targetType,
        string objectId,
        DateOnly from,
        DateOnly to)
    {
        IReadOnlyDictionary<string, IReadOnlyList<ScheduleItemVm>> grouped =
            ScheduleDisplayService.GroupByDate(response.Lessons);

        return new ScheduleResultVm
        {
            Title = title,
            IsWeeklyView = weeklyView,
            ExportUrl = weeklyView ? null : scheduleQuery.BuildExportUrl(targetType, objectId, from, to),
            GroupedByDate = weeklyView ? null : grouped,
            Weeks = weeklyView ? ScheduleDisplayService.CreateWeeklyView(grouped) : null,
        };
    }

    private void ApplyDefaultDates(ScheduleIndexViewModel model)
    {
        void Apply(DateRangeFormVm form)
        {
            PeriodOptionVm option = model.PeriodOptions.FirstOrDefault(item => item.Key == form.PeriodKey)
                                    ?? model.PeriodOptions.First(item => item.Key == "to_end_of_week");

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

    private static bool TryParseScheduleMode(string? value, out ScheduleTargetType targetType)
    {
        targetType = value?.Trim().ToLowerInvariant() switch
        {
            "teacher" or "teachers" => ScheduleTargetType.Teacher,
            "group" or "groups" => ScheduleTargetType.Group,
            "room" or "rooms" or "auditorium" or "auditoriums" => ScheduleTargetType.Room,
            _ => default,
        };

        return value?.Trim().ToLowerInvariant() is
            "teacher" or "teachers" or "group" or "groups" or
            "room" or "rooms" or "auditorium" or "auditoriums";
    }
}
