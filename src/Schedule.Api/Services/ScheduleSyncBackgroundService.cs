using Microsoft.Extensions.Options;
using Schedule.Api.Configuration;

namespace Schedule.Api.Services;

internal sealed class ScheduleSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ScheduleSyncOptions> options,
    ILogger<ScheduleSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ScheduleSyncOptions settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Schedule synchronization is disabled.");
            return;
        }

        if (!settings.RunOnStartup)
        {
            await DelayUntilNextRunAsync(settings, stoppingToken);
        }

        do
        {
            try
            {
                DateOnly today = DateOnly.FromDateTime(DateTime.Now);
                (DateOnly fromDate, DateOnly toDate) = settings.ResolvePeriod(today);

                using IServiceScope scope = scopeFactory.CreateScope();
                IScheduleSynchronizer synchronizer = scope.ServiceProvider
                    .GetRequiredService<IScheduleSynchronizer>();

                await synchronizer.SynchronizeAsync(fromDate, toDate, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Schedule synchronization failed.");
            }

            if (settings.RunOnce)
            {
                return;
            }

            await DelayUntilNextRunAsync(settings, stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested);
    }

    private static Task DelayUntilNextRunAsync(
        ScheduleSyncOptions settings,
        CancellationToken cancellationToken)
    {
        TimeSpan delay = TimeSpan.FromHours(Math.Max(1, settings.IntervalHours));
        return Task.Delay(delay, cancellationToken);
    }
}
