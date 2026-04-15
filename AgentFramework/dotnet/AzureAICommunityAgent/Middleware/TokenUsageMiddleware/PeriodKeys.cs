namespace AzureAICommunity.Agent.Middleware.TokenUsageMiddleware;

/// <summary>
/// Factory methods for common time-based quota period keys.
/// Pass one of these (or a custom delegate) as the <c>periodKeyFn</c>
/// argument of <see cref="TokenUsageMiddleware"/>.
/// </summary>
public static class PeriodKeys
{
    /// <summary>Returns a month-granularity key, e.g. <c>"2026-04"</c>.</summary>
    public static string Month() => DateTimeOffset.UtcNow.ToString("yyyy-MM");

    /// <summary>Returns a day-granularity key, e.g. <c>"2026-04-14"</c>.</summary>
    public static string Day() => DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");

    /// <summary>Returns a week-granularity key (ISO week number), e.g. <c>"2026-W15"</c>.</summary>
    public static string Week()
    {
        var now = DateTimeOffset.UtcNow;
        var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
        int week = cal.GetWeekOfYear(
            now.DateTime,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);
        return $"{now.Year:D4}-W{week:D2}";
    }
}
