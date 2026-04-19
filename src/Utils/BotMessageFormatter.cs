namespace TelegramMonitor;

public static class BotMessageFormatter
{
    public static string FormatNotifyMessage(int accountId, TelegramMessageRecord record, List<KeywordConfig> matchedRules)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<b>📢 关键词命中通知</b>");
        sb.AppendLine();
        sb.AppendLine($"<b>账号:</b> {accountId}号");

        var chatDisplay = HtmlEncode(record.ChatTitle ?? "-");
        if (!string.IsNullOrWhiteSpace(record.ChatUsername))
            chatDisplay += $" @{HtmlEncode(record.ChatUsername)}";
        sb.AppendLine($"<b>来源:</b> {chatDisplay}");

        var senderDisplay = HtmlEncode(record.SenderTitle ?? "-");
        if (!string.IsNullOrWhiteSpace(record.SenderUsername))
            senderDisplay += $" @{HtmlEncode(record.SenderUsername)}";
        sb.AppendLine($"<b>发送者:</b> {senderDisplay}");

        var keywordNames = matchedRules
            .Select(r => string.IsNullOrWhiteSpace(r.RuleName) ? r.KeywordPattern : r.RuleName)
            .Select(HtmlEncode);
        sb.AppendLine($"<b>命中关键词:</b> {string.Join(", ", keywordNames)}");

        var timeStr = record.MessageDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
        sb.AppendLine($"<b>时间:</b> {timeStr}");

        var link = BuildMessageLink(record);
        if (link != null)
            sb.AppendLine($"<b>链接:</b> {link}");

        sb.AppendLine($"<b>内容:</b> {HtmlEncode(Truncate(record.Text, 500) ?? "-")}");
        return sb.ToString();
    }

    public static string? BuildCallbackData(int accountId, TelegramMessageRecord record)
    {
        if (record.SenderId == null)
            return null;

        var data = $"blk:{accountId}:{record.SenderId}";
        return data.Length > 64 ? null : data;
    }

    private static string? BuildMessageLink(TelegramMessageRecord record)
    {
        if (record.TelegramMessageId <= 0 || record.ChatId == null)
            return null;

        if (!string.IsNullOrWhiteSpace(record.ChatUsername))
            return $"https://t.me/{record.ChatUsername}/{record.TelegramMessageId}";

        var chatId = record.ChatId.Value;
        if (chatId < 0)
            chatId = -chatId - 1000000000000;

        return $"https://t.me/c/{chatId}/{record.TelegramMessageId}";
    }

    private static string HtmlEncode(string? value) =>
        string.IsNullOrEmpty(value) ? "-" : value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
