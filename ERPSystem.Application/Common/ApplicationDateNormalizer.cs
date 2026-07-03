using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Common;

/// <summary>
/// WPF DatePicker values are Local/Unspecified; PostgreSQL timestamptz requires UTC.
/// </summary>
public static class ApplicationDateNormalizer
{
    public static DateTime ToUtcDate(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
            return value.Date;

        // Calendar date from UI — keep the same day in UTC.
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }

    public static DateTime? ToUtcDate(DateTime? value) =>
        value is null ? null : ToUtcDate(value.Value);

    public static DateTime? NextDueFromFrequency(DateTime startDate, ExpenseRecurrenceFrequency frequency, int? customDays = null)
    {
        var start = ToUtcDate(startDate);
        return frequency switch
        {
            ExpenseRecurrenceFrequency.Daily => start.AddDays(1),
            ExpenseRecurrenceFrequency.Weekly => start.AddDays(7),
            ExpenseRecurrenceFrequency.Monthly => start.AddMonths(1),
            ExpenseRecurrenceFrequency.Quarterly => start.AddMonths(3),
            ExpenseRecurrenceFrequency.Yearly => start.AddYears(1),
            ExpenseRecurrenceFrequency.Custom when customDays > 0 => start.AddDays(customDays.Value),
            _ => start.AddMonths(1)
        };
    }
}
