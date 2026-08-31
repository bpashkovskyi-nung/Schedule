using System.Globalization;
using System.Text.Json;
using global::Schedule.Contracts.Schedules;

namespace Schedule.Mobile.Services;

public sealed class MobileScheduleSettingsStore
{
    private const string TargetTypeKey = "mobile.schedule.target-type";
    private const string ObjectIdKey = "mobile.schedule.object-id";
    private const string ObjectNameKey = "mobile.schedule.object-name";
    private const string FromDateKey = "mobile.schedule.from-date";
    private const string ToDateKey = "mobile.schedule.to-date";
    private const string HidePastDatesKey = "mobile.schedule.hide-past-dates";
    private const string NotifyAboutNewLessonsKey = "mobile.schedule.notify-about-new-lessons";
    private const string KnownLessonKeysKey = "mobile.schedule.known-lesson-keys";
    private const string KnownLessonScopeKey = "mobile.schedule.known-lesson-scope";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public MobileScheduleSelection Load()
    {
        MobileScheduleSelection selection = new();

        if (Preferences.Default.ContainsKey(TargetTypeKey) &&
            Enum.TryParse<ScheduleTargetType>(Preferences.Default.Get(TargetTypeKey, string.Empty), out ScheduleTargetType targetType))
        {
            selection.TargetType = targetType;
        }

        selection.ObjectId = Preferences.Default.Get(ObjectIdKey, string.Empty);
        selection.ObjectName = Preferences.Default.Get(ObjectNameKey, string.Empty);
        selection.FromDate = ReadDate(FromDateKey, selection.FromDate);
        selection.ToDate = ReadDate(ToDateKey, selection.ToDate);
        selection.HidePastDates = Preferences.Default.Get(HidePastDatesKey, false);
        selection.NotifyAboutNewLessons = Preferences.Default.Get(NotifyAboutNewLessonsKey, false);

        return selection;
    }

    public void Save(MobileScheduleSelection selection)
    {
        Preferences.Default.Set(TargetTypeKey, selection.TargetType.ToString());
        Preferences.Default.Set(ObjectIdKey, selection.ObjectId);
        Preferences.Default.Set(ObjectNameKey, selection.ObjectName);
        Preferences.Default.Set(FromDateKey, selection.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Preferences.Default.Set(ToDateKey, selection.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Preferences.Default.Set(HidePastDatesKey, selection.HidePastDates);
        Preferences.Default.Set(NotifyAboutNewLessonsKey, selection.NotifyAboutNewLessons);
    }

    public IReadOnlySet<string> LoadKnownLessonKeys()
    {
        string json = Preferences.Default.Get(KnownLessonKeysKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        try
        {
            HashSet<string>? keys = JsonSerializer.Deserialize<HashSet<string>>(json, JsonOptions);
            return keys is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(keys, StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }

    public bool HasKnownLessonSnapshot => Preferences.Default.ContainsKey(KnownLessonKeysKey);

    public string LoadKnownLessonScope() => Preferences.Default.Get(KnownLessonScopeKey, string.Empty);

    public void SaveKnownLessonKeys(string scope, IEnumerable<string> keys)
    {
        string json = JsonSerializer.Serialize(keys.Distinct(StringComparer.Ordinal), JsonOptions);
        Preferences.Default.Set(KnownLessonKeysKey, json);
        Preferences.Default.Set(KnownLessonScopeKey, scope);
    }

    public void ClearKnownLessonKeys()
    {
        Preferences.Default.Remove(KnownLessonKeysKey);
        Preferences.Default.Remove(KnownLessonScopeKey);
    }

    private static DateOnly ReadDate(string key, DateOnly fallback)
    {
        string value = Preferences.Default.Get(key, string.Empty);
        return DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly date)
            ? date
            : fallback;
    }
}
