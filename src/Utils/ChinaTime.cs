namespace TelegramMonitor;

public static class ChinaTime
{
    private static readonly TimeZoneInfo TimeZone = ResolveTimeZone();

    public static DateTime Now => ToChinaTime(DateTime.UtcNow);

    public static DateTime ToChinaTime(DateTime value)
    {
        if (value == default)
            return default;

        var normalized = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };

        var chinaTime = TimeZoneInfo.ConvertTimeFromUtc(normalized, TimeZone);
        return DateTime.SpecifyKind(chinaTime, DateTimeKind.Unspecified);
    }

    public static DateTime? ToChinaTime(DateTime? value) =>
        value.HasValue ? ToChinaTime(value.Value) : null;

    public static DateTime ToChinaTime(DateTimeOffset value)
    {
        var chinaTime = TimeZoneInfo.ConvertTime(value, TimeZone).DateTime;
        return DateTime.SpecifyKind(chinaTime, DateTimeKind.Unspecified);
    }

    public static DateTime? ToChinaTime(DateTimeOffset? value) =>
        value.HasValue ? ToChinaTime(value.Value) : null;

    private static TimeZoneInfo ResolveTimeZone()
    {
        foreach (var timeZoneId in new[] { "China Standard Time", "Asia/Shanghai" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("China-Standard-Time", TimeSpan.FromHours(8), "北京时间", "北京时间");
    }
}
