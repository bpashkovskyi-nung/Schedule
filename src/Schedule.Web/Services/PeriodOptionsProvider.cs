using Schedule.Web.ViewModels;

namespace Schedule.Web.Services;

public sealed class PeriodOptionsProvider
{
    public IReadOnlyList<PeriodOptionVm> GetPeriodOptions()
    {
        DateTime today = DateTime.Today;
        DateTime weekStart = GetWeekStart(today);
        DateTime weekEnd = GetWeekEnd(today);
        int year = today.Year;
        int month = today.Month;
        DateTime monthStart = new(year, month, 1);
        DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);
        DateTime prevMonthStart = monthStart.AddMonths(-1);
        DateTime prevMonthEnd = monthStart.AddDays(-1);
        (DateTime termStart, DateTime termEnd) = GetCurrentTerm(today);

        return
        [
            new PeriodOptionVm("to_end_of_week", "До кінця тижня", today, weekEnd),
            new PeriodOptionVm("current_week", "Поточний тиждень", weekStart, weekEnd),
            new PeriodOptionVm("current_month", "Поточний місяць", monthStart, monthEnd),
            new PeriodOptionVm("previous_month", "Попередній місяць", prevMonthStart, prevMonthEnd),
            new PeriodOptionVm("current_term", "Поточний семестр", termStart, termEnd),
            new PeriodOptionVm("custom", "Власний період", today, today),
        ];
    }

    public static (DateOnly From, DateOnly To) ResolveDates(
        string? periodKey,
        DateOnly? fromDate,
        DateOnly? toDate,
        IReadOnlyList<PeriodOptionVm> options)
    {
        if (periodKey == "custom")
        {
            if (fromDate is null || toDate is null)
            {
                throw new ArgumentException("Для власного періоду потрібні дати.");
            }

            return (fromDate.Value, toDate.Value);
        }

        PeriodOptionVm? option = options.FirstOrDefault(o => o.Key == periodKey);
        if (option is null)
        {
            throw new ArgumentException("Невідомий період.");
        }

        return (option.FromDate, option.ToDate);
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        int dayOfWeek = (int)date.DayOfWeek;
        int diff = dayOfWeek == 0 ? -6 : 1 - dayOfWeek;
        return date.AddDays(diff);
    }

    private static DateTime GetWeekEnd(DateTime date) =>
        GetWeekStart(date).AddDays(6);

    private static (DateTime Start, DateTime End) GetCurrentTerm(DateTime date)
    {
        int month = date.Month;
        int day = date.Day;

        if ((month == 8 && day >= 20) || month is >= 9 and <= 12 || (month == 1 && day <= 15))
        {
            return (new DateTime(date.Year, 9, 1), new DateTime(date.Year, 12, 31));
        }

        return (new DateTime(date.Year, 2, 2), new DateTime(date.Year, 6, 30));
    }
}
