using Schedule.Client.Core.Api;
using Schedule.Contracts.Schedules;

namespace Schedule.Client.Core.Services;

public sealed class ScheduleState
{
    public ScheduleResponse? Result { get; private set; }
    public bool IsLoading { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task LoadAsync(
        IScheduleApiClient apiClient,
        ScheduleTargetType targetType,
        string objectId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        Result = null;

        try
        {
            Result = await apiClient.GetScheduleAsync(targetType, objectId, from, to, cancellationToken);
        }
        catch (Exception exception) when (exception is ScheduleApiException or HttpRequestException)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
