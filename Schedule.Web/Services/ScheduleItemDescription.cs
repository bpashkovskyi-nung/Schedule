using Dekanat.ScheduleSdk.Models;

namespace Schedule.Web.Services;

internal static class ScheduleItemDescription
{
    public static void Normalize(IReadOnlyList<ScheduleItem>? items)
    {
        foreach (ScheduleItem item in items ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.LessonDescription))
            {
                item.LessonDescription = Build(item);
            }
        }
    }

    private static string Build(ScheduleItem item)
    {
        List<string> parts = [];
        Add(parts, item.Title);
        Add(parts, string.IsNullOrWhiteSpace(item.Type) ? null : $"({item.Type.Trim()})");
        Add(parts, item.Half);
        Add(parts, item.Group);
        Add(parts, item.Teacher);
        Add(parts, item.AdditionalTeachers);
        Add(parts, item.Room);
        Add(parts, item.Replacement);
        Add(parts, item.Reservation);
        return string.Join(" ", parts);
    }

    private static void Add(ICollection<string> parts, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add(value.Trim());
        }
    }
}
