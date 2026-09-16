using Android.Content;

namespace Schedule.Mobile.Services;

[BroadcastReceiver(Enabled = true, Exported = false)]
[Android.App.IntentFilter(new[]
{
    MobileNotificationScheduler.PollAction,
    Intent.ActionBootCompleted,
    MobileNotificationScheduler.PackageReplacedAction
})]
public sealed class ScheduleNotificationReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null)
        {
            return;
        }

        if (intent?.Action is Intent.ActionBootCompleted or MobileNotificationScheduler.PackageReplacedAction)
        {
            MobileScheduleSelection selection = new MobileScheduleSettingsStore().Load();
            if (selection.HasSelection && selection.NotifyAboutNewLessons)
            {
                MobileNotificationScheduler.ScheduleHourly(context);
            }

            return;
        }

        if (intent?.Action != MobileNotificationScheduler.PollAction)
        {
            return;
        }

        PendingResult? pendingResult = GoAsync();
        if (pendingResult is not null)
        {
            _ = PollAsync(context, pendingResult);
        }
    }

    private static async Task PollAsync(Context context, PendingResult pendingResult)
    {
        try
        {
            await ScheduleNotificationWorker.RunAsync(context);
        }
        catch (Exception exception)
        {
            // A temporary API/network failure must not terminate the receiver
            // before it releases the broadcast wakelock.
            System.Diagnostics.Debug.WriteLine($"Schedule notification poll failed: {exception}");
        }
        finally
        {
            pendingResult.Finish();
        }
    }
}
