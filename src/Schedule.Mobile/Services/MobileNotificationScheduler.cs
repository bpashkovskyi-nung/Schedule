using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using global::Schedule.Client.Core.Api;
using global::Schedule.Contracts.Schedules;
using System.Runtime.Versioning;

namespace Schedule.Mobile.Services;

public sealed class MobileNotificationScheduler
{
    public const string PollAction = "ua.edu.nung.schedule.action.POLL_SCHEDULE";
    public const string PackageReplacedAction = "android.intent.action.MY_PACKAGE_REPLACED";

    private const string NotificationChannelId = "schedule-updates";
    private const int AlarmRequestCode = 1201;
    private const int NotificationRequestCode = 1202;
    private const int NotificationId = 1203;
    private const int NotificationPermissionRequestCode = 1204;
    private const long PollIntervalMilliseconds = 60 * 60 * 1000L;

    public void Enable()
    {
        Context context = Android.App.Application.Context;
        EnsureNotificationChannel(context);
        ScheduleHourly(context);
        RequestNotificationPermission();
    }

    public void Disable()
    {
        Context context = Android.App.Application.Context;
        AlarmManager? alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager is not null)
        {
            alarmManager.Cancel(CreateAlarmPendingIntent(context));
        }

        new MobileScheduleSettingsStore().ClearKnownLessonKeys();
    }

    public static void ScheduleHourly(Context context)
    {
        AlarmManager? alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager is null)
        {
            return;
        }

        PendingIntent pendingIntent = CreateAlarmPendingIntent(context);
        long firstTrigger = SystemClock.ElapsedRealtime() + PollIntervalMilliseconds;
        alarmManager.SetInexactRepeating(
            AlarmType.ElapsedRealtimeWakeup,
            firstTrigger,
            PollIntervalMilliseconds,
            pendingIntent);
    }

    internal static void ShowNewLessons(Context context, IReadOnlyList<LessonDto> lessons)
    {
        if (lessons.Count == 0)
        {
            return;
        }

        EnsureNotificationChannel(context);

        string details = string.Join(", ", lessons
            .Take(2)
            .Select(lesson => $"{lesson.Date:dd.MM} {lesson.LessonTime} {GetLessonName(lesson)}"));
        if (lessons.Count > 2)
        {
            details += $" та ще {lessons.Count - 2}";
        }

        string message = lessons.Count == 1
            ? $"Нова пара: {details}"
            : $"Нових пар: {lessons.Count}. {details}";

        NotificationCompat.Builder? builder = new NotificationCompat.Builder(context, NotificationChannelId);
        if (builder is null)
        {
            return;
        }

        builder.SetSmallIcon(Resource.Drawable.ic_notification_schedule);
        builder.SetContentTitle("НУНГ Розклад");
        builder.SetContentText(message);
        NotificationCompat.BigTextStyle bigTextStyle = new NotificationCompat.BigTextStyle();
        bigTextStyle.BigText(message);
        builder.SetStyle(bigTextStyle);
        builder.SetAutoCancel(true);
        builder.SetPriority(NotificationCompat.PriorityDefault);
        builder.SetShowWhen(true);

        string? packageName = context.PackageName;
        Intent? launchIntent = string.IsNullOrWhiteSpace(packageName)
            ? null
            : context.PackageManager?.GetLaunchIntentForPackage(packageName);
        if (launchIntent is not null)
        {
            launchIntent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            PendingIntent? contentIntent = PendingIntent.GetActivity(
                context,
                NotificationRequestCode,
                launchIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            if (contentIntent is not null)
            {
                builder.SetContentIntent(contentIntent);
            }
        }

        try
        {
            NotificationManagerCompat? notificationManager = NotificationManagerCompat.From(context);
            notificationManager?.Notify(NotificationId, builder.Build());
        }
        catch (Java.Lang.SecurityException)
        {
            // The user may have denied POST_NOTIFICATIONS permission.
        }
    }

    private static void EnsureNotificationChannel(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        NotificationManager? manager = context.GetSystemService(Context.NotificationService) as NotificationManager;
        if (manager is null)
        {
            return;
        }

        NotificationChannel channel = new NotificationChannel(
            NotificationChannelId,
            "Оновлення розкладу",
            NotificationImportance.Default);
        channel.Description = "Сповіщення про нові пари в розкладі";
        manager.CreateNotificationChannel(channel);
    }

    private static void RequestNotificationPermission()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33) ||
            Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not Activity activity ||
            ContextCompat.CheckSelfPermission(activity, Android.Manifest.Permission.PostNotifications) == Permission.Granted)
        {
            return;
        }

        activity.RequestPermissions(
            [Android.Manifest.Permission.PostNotifications],
            NotificationPermissionRequestCode);
    }

    private static PendingIntent CreateAlarmPendingIntent(Context context)
    {
        Intent intent = new(context, typeof(ScheduleNotificationReceiver));
        intent.SetAction(PollAction);
        return PendingIntent.GetBroadcast(
            context,
            AlarmRequestCode,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)
            ?? throw new InvalidOperationException("Не вдалося створити Android PendingIntent для опитування розкладу.");
    }

    private static string GetLessonName(LessonDto lesson) =>
        new[] { lesson.LessonDescription, lesson.Title, lesson.LessonName }
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "Заняття";
}
