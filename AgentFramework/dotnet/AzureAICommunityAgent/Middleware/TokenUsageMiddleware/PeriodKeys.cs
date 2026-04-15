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
        int isoYear = System.Globalization.ISOWeek.GetYear(now.UtcDateTime);
        int week = System.Globalization.ISOWeek.GetWeekOfYear(now.UtcDateTime);
        return $"{isoYear:D4}-W{week:D2}";
    }
}
