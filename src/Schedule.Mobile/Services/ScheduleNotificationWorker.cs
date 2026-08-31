using System.Globalization;
using System.Net.Http;
using global::Schedule.Client.Core.Api;
using global::Schedule.Contracts.Schedules;

namespace Schedule.Mobile.Services;

internal static class ScheduleNotificationWorker
{
    public static async Task RunAsync(Android.Content.Context context)
    {
        MobileScheduleSettingsStore settingsStore = new();
        MobileScheduleSelection selection = settingsStore.Load();
        if (!selection.HasSelection || !selection.NotifyAboutNewLessons)
        {
            return;
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly from = selection.FromDate > today ? selection.FromDate : today;
        if (from > selection.ToDate)
        {
            settingsStore.ClearKnownLessonKeys();
            return;
        }

        using HttpClient httpClient = new()
        {
            BaseAddress = MobileApiConfiguration.BaseUri,
            Timeout = TimeSpan.FromSeconds(45)
        };
        ScheduleApiClient apiClient = new(httpClient);
        ScheduleResponse response = await apiClient.GetScheduleAsync(
            selection.TargetType,
            selection.ObjectId,
            from,
            selection.ToDate);

        List<LessonDto> lessons = response.Lessons
            .Where(IsNotifiableLesson)
            .ToList();
        HashSet<string> currentKeys = lessons
            .Select(GetLessonKey)
            .ToHashSet(StringComparer.Ordinal);
        string scope = GetScope(selection);
        bool hasBaseline = settingsStore.HasKnownLessonSnapshot &&
                           string.Equals(settingsStore.LoadKnownLessonScope(), scope, StringComparison.Ordinal);

        if (hasBaseline)
        {
            IReadOnlySet<string> knownKeys = settingsStore.LoadKnownLessonKeys();
            List<LessonDto> newLessons = lessons
                .Where(lesson => !knownKeys.Contains(GetLessonKey(lesson)))
                .ToList();
            MobileNotificationScheduler.ShowNewLessons(context, newLessons);
        }

        settingsStore.SaveKnownLessonKeys(scope, currentKeys);
    }

    private static bool IsNotifiableLesson(LessonDto lesson) =>
        lesson.Date.HasValue &&
        !string.IsNullOrWhiteSpace(lesson.LessonDescription);

    private static string GetScope(MobileScheduleSelection selection) =>
        string.Join("\u001f", selection.TargetType, selection.ObjectId, selection.FromDate, selection.ToDate);

    private static string GetLessonKey(LessonDto lesson) =>
        string.Join("\u001f",
            lesson.Date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            lesson.LessonNumber,
            lesson.LessonTime,
            lesson.LessonDescription,
            lesson.Title,
            lesson.LessonName,
            lesson.Room,
            lesson.Teacher,
            lesson.Group,
            lesson.Type,
            lesson.Replacement,
            lesson.Online);
}
