using Dekanat.ScheduleSdk.Exceptions;
using Schedule.Contracts.Common;

namespace Schedule.Api.Middleware;

public sealed class ApiExceptionMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (PsRozkladApiException exception)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status502BadGateway,
                "external_schedule_api_error",
                exception.ErrorMessage ?? "Зовнішній сервіс розкладу повернув помилку.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "External HTTP request failed while serving {Path}", context.Request.Path);
            await WriteErrorAsync(
                context,
                StatusCodes.Status502BadGateway,
                "external_service_unavailable",
                "Зовнішній сервіс тимчасово недоступний.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception while serving {Path}", context.Request.Path);
            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "internal_server_error",
                "Внутрішня помилка сервера.");
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(
            new ApiErrorResponse(code, message, context.TraceIdentifier));
    }
}
