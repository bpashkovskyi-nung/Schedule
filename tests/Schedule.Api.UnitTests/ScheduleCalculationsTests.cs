using Dekanat.ScheduleSdk.Models;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Schedule.Api.Configuration;
using Schedule.Api.Services;
using Schedule.Contracts.ReferenceData;
using Schedule.Contracts.Schedules;
using Schedule.Contracts.Workload;
using Xunit;

namespace Schedule.Api.UnitTests;

public sealed class ScheduleCalculationsTests
{
    [Fact]
    public void CountWorkingDays_ExcludesWeekend()
    {
        ScheduleCalculations.CountWorkingDays(
                new DateOnly(2026, 8, 24),
                new DateOnly(2026, 8, 30))
            .Should().Be(5);
    }

    [Fact]
    public void CountActualPairs_DeduplicatesSameDateAndPair()
    {
        ScheduleItem[] items =
        [
            new() { Date = "24.08.2026", LessonNumber = "1", LessonDescription = "(Л) Математика" },
            new() { Date = "24.08.2026", LessonNumber = "1", LessonDescription = "(Л) Математика" },
            new() { Date = "25.08.2026", LessonNumber = "2", LessonDescription = "(Пр) Фізика" },
            new() { Date = "25.08.2026", LessonNumber = "9", LessonDescription = "(Л) Поза діапазоном" },
        ];

        ScheduleCalculations.CountActualPairs(items, pairsFrom: 1, pairsTo: 8)
            .Should().Be(2);
    }

    [Fact]
    public void BuildTeachingLoad_CreatesColumnsAndTotals()
    {
        ScheduleEntityDto[] teachers =
        [
            new() { Id = "teacher-1", Name = "Іваненко Іван", FullName = "Іваненко Іван" },
            new() { Id = "teacher-2", Name = "Петренко Петро", FullName = "Петренко Петро" },
        ];

        PsRozkladExport[] schedules =
        [
            new()
            {
                ScheduleItems =
                [
                    new() { LessonDescription = "(Л) Алгебра" },
                    new() { LessonDescription = "(Пр) Програмування" },
                ],
            },
            new() { ScheduleItems = [new() { LessonDescription = "(Л) Алгебра" }] },
        ];

        TeachingLoadResponse result = ScheduleCalculations.BuildTeachingLoad(teachers, schedules);

        result.SessionTypes.Should().Equal("(Л)", "(Пр)");
        result.Rows[0].HoursByType.Should().Equal(new Dictionary<string, int>
        {
            ["(Л)"] = 2,
            ["(Пр)"] = 2,
        });
        result.Rows[0].TotalHours.Should().Be(4);
        result.Rows[1].TotalHours.Should().Be(2);
    }

    [Fact]
    public void BuildIcalUrl_UsesConfiguredTimetableBaseUrl()
    {
        ScheduleApiService service = new(
            null!,
            Options.Create(new ScheduleApiOptions
            {
                TimetableBaseUrl = "https://api.test.local/timetable.cgi",
            }),
            new MemoryCache(new MemoryCacheOptions()));

        string url = service.BuildIcalUrl(
            ScheduleTargetType.Group,
            "group/1",
            new DateOnly(2026, 8, 24),
            new DateOnly(2026, 8, 30));

        url.Should().StartWith("https://api.test.local/timetable.cgi?");
        url.Should().Contain("req_mode=group");
        url.Should().Contain("OBJ_ID=group%2F1");
        url.Should().Contain("begin_date=24.08.2026");
    }
}
