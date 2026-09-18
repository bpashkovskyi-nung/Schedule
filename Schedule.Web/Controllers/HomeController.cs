using Microsoft.AspNetCore.Mvc;
using Schedule.Client.Core.Api;
using Schedule.Contracts.AuditoriumLoad;
using Schedule.Contracts.Schedules;
using Schedule.Contracts.Workload;
using Schedule.Web.Services;
using Schedule.Web.ViewModels;

namespace Schedule.Web.Controllers;

public sealed class HomeController(
    IScheduleReferenceService referenceService,
    IScheduleQueryService scheduleQuery,
    PeriodOptionsProvider periodOptions) : Controller
{
    private static readonly HashSet<string> AvailableTabs =
    [
        "rooms",
        "auditorium-load",
        "teachers",
        "groups",
        "teaching-load",
    ];

    [HttpGet]
    public IActionResult Error() => View();

    [HttpGet]
    public IActionResult Privacy() => View();

    [HttpGet]
    public async Task<IActionResult> Index(string? tab, CancellationToken cancellationToken)
    {
        ScheduleIndexViewModel model = await BuildBaseModelAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(tab) && AvailableTabs.Contains(tab))
        {
            model.ActiveTab = tab;
        }

        ApplyDefaultDates(model);
        return View(model);
    }

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
                response, title, ScheduleTargetType.Room, form.RoomId, from, to);
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
                response, title, ScheduleTargetType.Teacher, form.TeacherId, from, to);
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
                response, title, ScheduleTargetType.Group, form.GroupId, from, to);
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

    private ScheduleResultVm BuildScheduleResult(
        ScheduleResponse response,
        string title,
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
            ExportUrl = scheduleQuery.BuildExportUrl(targetType, objectId, from, to),
            GroupedByDate = grouped,
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
