using Dekanat.ScheduleSdk.Models;
using Dekanat.ScheduleSdk.Enums;
using Dekanat.ScheduleSdk.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Schedule.Api.Configuration;
using Schedule.Api.Services;
using Schedule.Contracts.AuditoriumLoad;
using Schedule.Contracts.Common;
using Schedule.Contracts.ReferenceData;
using Schedule.Contracts.Schedules;
using Schedule.Contracts.Workload;

namespace Schedule.Api.Controllers;

[Route("api/v1/workload")]
[EnableRateLimiting("workload")]
public sealed class WorkloadController(
    IScheduleApiService scheduleService,
    IOptions<ScheduleApiOptions> options) : ApiControllerBase
{
    private readonly ScheduleApiOptions apiOptions = options.Value;

    [HttpGet("teachers")]
    public async Task<ActionResult<TeachingLoadResponse>> GetTeachers(
        [FromQuery] string department,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(department))
        {
            return BadRequest(new ApiErrorResponse("department_required", "Необхідно передати підрозділ."));
        }

        ApiErrorResponse? periodError = InvalidPeriod(from, to);
        if (periodError is not null) return BadRequest(periodError);

        try
        {
            ReferenceDataResponse reference = await scheduleService.GetReferenceDataAsync(cancellationToken);
            DepartmentDto? selected = reference.TeacherDepartments.FirstOrDefault(x => x.Name == department);
            if (selected is null)
            {
                return NotFound(new ApiErrorResponse("department_not_found", "Підрозділ не знайдено."));
            }

            List<PsRozkladExport> schedules = new(selected.Entities.Count);
            foreach (ScheduleEntityDto teacher in selected.Entities)
            {
                try
                {
                    schedules.Add(await scheduleService.GetScheduleExportAsync(
                        ScheduleTargetType.Teacher,
                        teacher.Id,
                        from,
                        to,
                        cancellationToken));
                }
                catch (PsRozkladApiException exception) when (
                    exception.ErrorCode == (int)ApiResultCode.NotFound)
                {
                    // A teacher without lessons in the selected period still belongs in the report.
                    schedules.Add(new PsRozkladExport { Code = 0, ScheduleItems = [] });
                }
            }

            return Ok(ScheduleCalculations.BuildTeachingLoad(selected.Entities, schedules));
        }
        catch (PsRozkladApiException exception)
        {
            return ExternalApiError<TeachingLoadResponse>(exception);
        }
    }

    [HttpGet("auditoriums")]
    public async Task<ActionResult<AuditoriumLoadResponse>> GetAuditoriums(
        [FromQuery] string? building,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int pairsFrom = 1,
        [FromQuery] int pairsTo = 8,
        CancellationToken cancellationToken = default)
    {
        if (pairsFrom < 1 || pairsTo < pairsFrom)
        {
            return BadRequest(new ApiErrorResponse("invalid_pairs_range", "Діапазон пар некоректний."));
        }

        ApiErrorResponse? periodError = InvalidPeriod(from, to);
        if (periodError is not null) return BadRequest(periodError);

        try
        {
            ReferenceDataResponse reference = await scheduleService.GetReferenceDataAsync(cancellationToken);
            IReadOnlyList<BuildingDto> buildings = string.IsNullOrWhiteSpace(building)
                ? reference.Buildings
                : reference.Buildings.Where(x => x.Name == building).ToList();

            int workingDays = ScheduleCalculations.CountWorkingDays(from, to);
            int possiblePairs = workingDays * (pairsTo - pairsFrom + 1);
            List<RoomTarget> rooms = buildings
                .SelectMany(x => x.Rooms.Select(room => new RoomTarget(room.Id, room.Name, x.Name)))
                .DistinctBy(x => x.Id)
                .ToList();

            int errorCount = 0;
            using SemaphoreSlim semaphore = new(Math.Max(1, apiOptions.AuditoriumConcurrency));
            Task<AuditoriumLoadItemDto>[] tasks = rooms.Select(async room =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    PsRozkladExport export = await scheduleService.GetScheduleExportAsync(
                        ScheduleTargetType.Room, room.Id, from, to, cancellationToken);
                    int actualPairs = ScheduleCalculations.CountActualPairs(export.ScheduleItems, pairsFrom, pairsTo);
                    return new AuditoriumLoadItemDto
                    {
                        RoomName = room.Name,
                        BuildingName = room.Building,
                        Percent = possiblePairs > 0 ? actualPairs * 100.0 / possiblePairs : 0,
                        ActualPairs = actualPairs,
                        PossiblePairs = possiblePairs,
                        Lessons = ScheduleCalculations.BuildAuditoriumLessons(export.ScheduleItems),
                    };
                }
                catch (PsRozkladApiException)
                {
                    Interlocked.Increment(ref errorCount);
                    return new AuditoriumLoadItemDto
                    {
                        RoomName = room.Name,
                        BuildingName = room.Building,
                        Percent = 0,
                        ActualPairs = 0,
                        PossiblePairs = possiblePairs,
                        Lessons = [],
                    };
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            AuditoriumLoadItemDto[] items = await Task.WhenAll(tasks);
            return Ok(new AuditoriumLoadResponse
            {
                Items = ScheduleCalculations.SortLoadItems(items),
                ErrorCount = errorCount,
                WorkingDays = workingDays,
                PossiblePairsPerRoom = possiblePairs,
            });
        }
        catch (PsRozkladApiException exception)
        {
            return ExternalApiError<AuditoriumLoadResponse>(exception);
        }
    }

    private sealed record RoomTarget(string Id, string Name, string Building);
}
