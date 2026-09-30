namespace Schedule.Api.Services;

internal interface IScheduleSynchronizer
{
    Task SynchronizeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);
}
