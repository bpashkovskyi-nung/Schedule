using System.Globalization;
using System.Text.RegularExpressions;
using Schedule.Contracts.Schedules;
using Schedule.Web.ViewModels;

namespace Schedule.Web.Services;

internal static class ScheduleDisplayService
{
    internal static IReadOnlyDictionary<string, IReadOnlyList<ScheduleItemVm>> GroupByDate(
        IReadOnlyList<LessonDto>? items)
    {
        if (items is null || items.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<ScheduleItemVm>>();
        }

        return items
            .Where(item => item.Date.HasValue && !string.IsNullOrWhiteSpace(item.LessonDescription))
            .GroupBy(item => item.Date!.Value)
            .OrderBy(group => group.Key)
            .ToDictionary(
                group => group.Key.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                group => (IReadOnlyList<ScheduleItemVm>)group.Select(MapItem).ToList());
    }

    private static ScheduleItemVm MapItem(LessonDto item) => new()
    {
        LessonNumber = item.LessonNumber,
        LessonTime = item.LessonTime,
        LessonDescription = item.LessonDescription,
        Links = ParseLinks(item.Link, item.LinkComment),
    };

    private static IReadOnlyList<ScheduleLinkVm> ParseLinks(string? value, string? comment)
    {
        if (string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(comment))
        {
            return [];
        }

        string source = string.Join("\n", new[] { value, comment }.Where(item => !string.IsNullOrWhiteSpace(item)));
        List<string> urls = Regex.Matches(source, @"https?://[^\s<>;,""']+", RegexOptions.IgnoreCase)
            .Select(match => TrimUrl(match.Value))
            .Where(IsHttpUrl)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string label = GetLinkLabel(comment);
        return urls.Select((url, index) => new ScheduleLinkVm
        {
            Url = url,
            Label = urls.Count == 1 ? label : $"{label} {index + 1}",
        }).ToList();
    }

    private static string GetLinkLabel(string? comment) =>
        string.IsNullOrWhiteSpace(comment)
            ? "Онлайн-заняття"
            : Regex.Replace(comment, @"https?://[^\s<>;,""']+", string.Empty, RegexOptions.IgnoreCase)
                .Trim(' ', '\t', '\r', '\n', ':', ';', ',') is { Length: > 0 } label
                ? label
                : "Онлайн-заняття";

    private static string TrimUrl(string value) => value.TrimEnd('.', ',', ';', ':', ')', ']', '}', '"', '\'');

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

}
