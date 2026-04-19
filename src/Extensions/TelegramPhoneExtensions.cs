namespace TelegramMonitor;

public static class TelegramPhoneExtensions
{
    public static string NormalizeTelegramPhone(this string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var builder = new StringBuilder(phone.Length);
        foreach (var ch in phone)
        {
            if (ch is >= '0' and <= '9')
                builder.Append(ch);
        }

        return builder.ToString();
    }
}
