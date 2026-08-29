namespace Schedule.Client.Core.Api;

public sealed class ScheduleApiException(
    string message,
    int? statusCode = null,
    string? code = null,
    Exception? innerException = null) : Exception(message, innerException)
{
    public int? StatusCode { get; } = statusCode;
    public string? Code { get; } = code;
}
