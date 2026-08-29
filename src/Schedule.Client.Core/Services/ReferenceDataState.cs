using Schedule.Client.Core.Api;
using Schedule.Contracts.ReferenceData;

namespace Schedule.Client.Core.Services;

public sealed class ReferenceDataState
{
    private readonly SemaphoreSlim loadLock = new(1, 1);

    public ReferenceDataResponse? Data { get; private set; }
    public bool IsLoading { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task EnsureLoadedAsync(IScheduleApiClient apiClient, CancellationToken cancellationToken = default)
    {
        if (Data is not null) return;

        await loadLock.WaitAsync(cancellationToken);
        try
        {
            if (Data is not null) return;

            IsLoading = true;
            ErrorMessage = null;
            try
            {
                Data = await apiClient.GetReferenceDataAsync(cancellationToken);
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
        finally
        {
            loadLock.Release();
        }
    }
}
