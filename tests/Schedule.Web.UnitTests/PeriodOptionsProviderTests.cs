using FluentAssertions;
using Schedule.Web.Services;
using Schedule.Web.ViewModels;
using Xunit;

namespace Schedule.Web.UnitTests;

public sealed class PeriodOptionsProviderTests
{
    private readonly PeriodOptionsProvider _provider = new();

    [Fact]
    public void GetPeriodOptions_ShouldReturnSixOptions()
    {
        IReadOnlyList<PeriodOptionVm> options = _provider.GetPeriodOptions();
        options.Should().HaveCount(6);
        options.Should().Contain(o => o.Key == "custom");
    }

    [Theory]
    [InlineData("current_week")]
    [InlineData("to_end_of_week")]
    public void ResolveDates_WithPresetPeriod_ShouldUseOptionRange(string periodKey)
    {
        IReadOnlyList<PeriodOptionVm> options = _provider.GetPeriodOptions();
        PeriodOptionVm option = options.First(o => o.Key == periodKey);

        (DateOnly from, DateOnly to) = PeriodOptionsProvider.ResolveDates(periodKey, null, null, options);

        from.Should().Be(option.FromDate);
        to.Should().Be(option.ToDate);
    }

    [Fact]
    public void ResolveDates_WithCustomPeriod_ShouldUseProvidedDates()
    {
        IReadOnlyList<PeriodOptionVm> options = _provider.GetPeriodOptions();
        DateOnly from = new(2026, 3, 1);
        DateOnly to = new(2026, 3, 15);

        (DateOnly resolvedFrom, DateOnly resolvedTo) =
            PeriodOptionsProvider.ResolveDates("custom", from, to, options);

        resolvedFrom.Should().Be(from);
        resolvedTo.Should().Be(to);
    }
}
