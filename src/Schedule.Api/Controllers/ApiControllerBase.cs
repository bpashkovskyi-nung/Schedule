using Dekanat.ScheduleSdk.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Schedule.Contracts.Common;
using Schedule.Contracts.Schedules;

namespace Schedule.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected static bool TryParseTargetType(string value, out ScheduleTargetType targetType)
    {
        targetType = value.Trim().ToLowerInvariant() switch
        {
            "teacher" or "teachers" => ScheduleTargetType.Teacher,
            "group" or "groups" => ScheduleTargetType.Group,
            "room" or "rooms" or "auditorium" or "auditoriums" => ScheduleTargetType.Room,
            _ => default,
        };

        return value.Trim().ToLowerInvariant() is
            "teacher" or "teachers" or "group" or "groups" or
            "room" or "rooms" or "auditorium" or "auditoriums";
    }

    protected ApiErrorResponse? InvalidPeriod(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            return new ApiErrorResponse("invalid_period", "Дата початку не може бути пізнішою за дату завершення.");
        }

        return null;
    }

    protected ActionResult<T> ExternalApiError<T>(PsRozkladApiException exception)
    {
        return StatusCode(StatusCodes.Status502BadGateway, new ApiErrorResponse(
            "external_schedule_api_error",
            exception.ErrorMessage ?? "Зовнішній сервіс розкладу повернув помилку.",
            HttpContext.TraceIdentifier));
    }
}
