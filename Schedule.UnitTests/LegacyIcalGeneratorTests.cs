using System.Text;
using Dekanat.ScheduleSdk.Models;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Schedule.Web.Configuration;
using Schedule.Web.Services;
using Xunit;

namespace Schedule.UnitTests;

public sealed class LegacyIcalGeneratorTests
{
    [Fact]
    public void Generate_should_create_timed_event_from_schedule_json()
    {
        PsRozkladExport export = new()
        {
            ScheduleItems =
            [
                new ScheduleItem
                {
                    Date = "24.08.2026",
                    LessonNumber = "1",
                    LessonTime = "08:00-09:20",
                    LessonDescription = "(Л) Математика",
                    Title = "Математика",
                    Room = "101",
                    Teacher = "Іваненко І.І.",
                },
            ],
        };

        string ical = Encoding.UTF8.GetString(new LegacyIcalGenerator().Generate(export, "Група АЕ-26-1"));

        ical.Should().Contain("BEGIN:VCALENDAR\r\n");
        ical.Should().Contain("BEGIN:VEVENT\r\n");
        ical.Should().Contain("SUMMARY:Математика\r\n");
        ical.Should().Contain("LOCATION:101\r\n");
        ical.Should().Contain("DTSTART:");
        ical.Should().Contain("DTEND:");
        ical.Should().Contain("END:VCALENDAR\r\n");
        ical.Should().NotContain("req_format=iCal");
    }

    [Fact]
    public void Generate_should_include_all_schedule_fields_in_event_description()
    {
        PsRozkladExport export = new()
        {
            ScheduleItems =
            [
                new ScheduleItem
                {
                    ObjectName = "Група АЕ-26-1",
                    Date = "24.08.2026",
                    LessonNumber = "2",
                    LessonName = "2",
                    LessonTime = "09:30-10:50",
                    LessonDescription = "Математика (Пр)",
                    Title = "Математика",
                    Type = "Пр",
                    Half = "підгр. 1",
                    Teacher = "Іваненко І.І.",
                    AdditionalTeachers = "Петренко П.П.",
                    Group = "Потік АЕ-26-1",
                    Room = "101",
                    Online = "Так",
                    LinkComment = "Посилання для групи",
                    Link = "https://meet.example/lesson",
                    Replacement = "Заміна викладача",
                    Reservation = "Зарезервовано",
                    Comment = "Принести звіт",
                },
            ],
        };

        string ical = Encoding.UTF8.GetString(new LegacyIcalGenerator().Generate(export, "Група АЕ-26-1"));
        string unfolded = ical.Replace("\r\n ", string.Empty, StringComparison.Ordinal);

        unfolded.Should().Contain("SUMMARY:Математика (Пр)\r\n");
        unfolded.Should().Contain("Об'єкт: Група АЕ-26-1\\n");
        unfolded.Should().Contain("Номер пари: 2\\n");
        unfolded.Should().Contain("Назва пари: 2\\n");
        unfolded.Should().Contain("Час: 09:30-10:50\\n");
        unfolded.Should().Contain("Тип заняття: Пр\\n");
        unfolded.Should().Contain("Підгрупа: підгр. 1\\n");
        unfolded.Should().Contain("Додаткові викладачі: Петренко П.П.\\n");
        unfolded.Should().Contain("Посилання: https://meet.example/lesson\\n");
        unfolded.Should().Contain("Резервування: Зарезервовано\\n");
    }

    [Fact]
    public void Generate_should_fold_long_ical_lines()
    {
        PsRozkladExport export = new()
        {
            ScheduleItems =
            [
                new ScheduleItem
                {
                    Date = "24.08.2026",
                    LessonDescription = new string('А', 400),
                    LessonTime = "08:00-09:20",
                },
            ],
        };

        string ical = Encoding.UTF8.GetString(new LegacyIcalGenerator().Generate(export, "Група"));
        string[] lines = ical.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        lines.Should().OnlyContain(line => Encoding.UTF8.GetByteCount(line) <= 75);
        ical.Should().Contain("\r\n ");
    }

    [Fact]
    public void Generate_should_fall_back_to_all_day_event_without_time()
    {
        PsRozkladExport export = new()
        {
            ScheduleItems =
            [
                new ScheduleItem
                {
                    Date = "24.08.2026",
                    LessonDescription = "Самостійна робота",
                },
            ],
        };

        string ical = Encoding.UTF8.GetString(new LegacyIcalGenerator().Generate(export, "Група"));

        ical.Should().Contain("DTSTART;VALUE=DATE:20260824\r\n");
        ical.Should().Contain("DTEND;VALUE=DATE:20260825\r\n");
    }

    [Fact]
    public void Generate_should_skip_items_without_description()
    {
        PsRozkladExport export = new()
        {
            ScheduleItems =
            [
                new ScheduleItem
                {
                    Date = "24.08.2026",
                    LessonNumber = "1",
                    LessonTime = "08:00-09:20",
                },
                new ScheduleItem
                {
                    Date = "24.08.2026",
                    LessonNumber = "2",
                    LessonTime = "09:30-10:50",
                    LessonDescription = "Програмування",
                },
            ],
        };

        string ical = Encoding.UTF8.GetString(new LegacyIcalGenerator().Generate(export, "Група"));

        ical.Should().Contain("SUMMARY:Програмування\r\n");
        ical.Should().NotContain("SUMMARY:Заняття\r\n");
        ical.Should().Contain("BEGIN:VEVENT\r\n").And.Contain("END:VEVENT\r\n");
        ical.Split("BEGIN:VEVENT\r\n").Length.Should().Be(2);
    }

    [Fact]
    public void BuildExportUrl_should_use_local_ical_endpoint_when_feature_is_enabled()
    {
        ScheduleQueryService service = new(
            null!,
            Options.Create(new LegacyFeatureOptions { GenerateIcalFromJson = true }));

        string url = service.BuildExportUrl(
            Dekanat.ScheduleSdk.Enums.RequestMode.Group,
            "Група А/1",
            new DateOnly(2026, 8, 24),
            new DateOnly(2026, 8, 30));

        url.Should().StartWith("/Home/Ical?");
        url.Should().Contain("mode=group");
        url.Should().Contain("objectId=%D0%93%D1%80%D1%83%D0%BF%D0%B0%20%D0%90%2F1");
        url.Should().Contain("from=2026-08-24");
        url.Should().Contain("to=2026-08-30");
    }

    [Fact]
    public void BuildExportUrl_should_keep_legacy_direct_export_when_feature_is_disabled()
    {
        ScheduleQueryService service = new(
            null!,
            Options.Create(new LegacyFeatureOptions { GenerateIcalFromJson = false }));

        string url = service.BuildExportUrl(
            Dekanat.ScheduleSdk.Enums.RequestMode.Group,
            "group-1",
            new DateOnly(2026, 8, 24),
            new DateOnly(2026, 8, 30));

        url.Should().Contain("timetable_export.cgi");
        url.Should().Contain("req_format=iCal");
    }
}
