using Dekanat.ScheduleSdk.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Schedule.Api.Configuration;
using Schedule.Api.Services;
using Schedule.Contracts.Common;
using Schedule.Contracts.Schedules;

namespace Schedule.Api.Controllers;

[Route("api/v1/schedules")]
public sealed class SchedulesController(
    IScheduleApiService scheduleService,
    IOptions<ScheduleApiOptions> options) : ApiControllerBase
{
    private readonly ScheduleApiOptions apiOptions = options.Value;

    [HttpGet("{targetType}/{objectId}")]
    [ProducesResponseType<ScheduleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScheduleResponse>> Get(
        string targetType,
        string objectId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (!TryParseTargetType(targetType, out ScheduleTargetType parsedTarget))
        {
            return BadRequest(new ApiErrorResponse("invalid_target_type", "Підтримуються teacher, group або room."));
        }

        if (string.IsNullOrWhiteSpace(objectId))
        {
            return BadRequest(new ApiErrorResponse("object_id_required", "Необхідно передати ідентифікатор об’єкта."));
        }

        ApiErrorResponse? periodError = InvalidPeriod(from, to);
        if (periodError is not null)
        {
            return BadRequest(periodError);
        }

        try
        {
            return Ok(await scheduleService.GetScheduleAsync(
                parsedTarget,
                objectId,
                from,
                to,
                cancellationToken));
        }
        catch (PsRozkladApiException exception)
        {
            return ExternalApiError<ScheduleResponse>(exception);
        }
    }

    [HttpGet("{targetType}/{objectId}/ical")]
    public async Task<ActionResult> GetIcal(
        string targetType,
        string objectId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (!TryParseTargetType(targetType, out ScheduleTargetType parsedTarget) ||
            string.IsNullOrWhiteSpace(objectId))
        {
            return BadRequest(new ApiErrorResponse("invalid_schedule_target", "Некоректний тип або ідентифікатор об’єкта."));
        }

        ApiErrorResponse? periodError = InvalidPeriod(from, to);
        if (periodError is not null)
        {
            return BadRequest(periodError);
        }

        if (scheduleService.IsDatabaseSource || apiOptions.GenerateIcalFromJson)
        {
            byte[] calendar = await scheduleService.GenerateIcalAsync(
                parsedTarget,
                objectId,
                from,
                to,
                cancellationToken);

            return File(
                calendar,
                "text/calendar; charset=utf-8",
                SanitizeFileName(objectId) + ".ics");
        }

        return Redirect(scheduleService.BuildIcalUrl(parsedTarget, objectId, from, to));
    }

    private static string SanitizeFileName(string value)
    {
        string fileName = string.Concat(value.Where(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' or ' '));
        fileName = fileName.Trim().Replace(' ', '_');
        return string.IsNullOrWhiteSpace(fileName) ? "schedule" : fileName;
    }
}
