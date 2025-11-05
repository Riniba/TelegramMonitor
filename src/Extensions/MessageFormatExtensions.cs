namespace TelegramMonitor;

public static class MessageFormatExtensions
{
    private static readonly char[] MdV2Reserved = new[] {
    '_','*','[',']','(',')','~','`','>','#','+','-','=','|','{','}','.','!'};

    private static string EscapeMdV2(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new StringBuilder(s.Length * 2);
        foreach (var ch in s)
        {
            if (Array.IndexOf(MdV2Reserved, ch) >= 0) sb.Append('\\');
            sb.Append(ch);
        }
        return sb.ToString();
    }

    public static string FormatForMonitor(this Message message,
        SendMessageEntity sendMessageEntity,
        IReadOnlyList<KeywordConfig> hitKeywords)
    {
        var mergedStyle = MergeKeywordStyles(hitKeywords);
        var styledText = ApplyStylesToText(message.message, mergedStyle);

        var keywordList = string.Join(", ",
            hitKeywords.Select(k => $"\\#{EscapeMdV2(k.KeywordContent)}"));

        var sb = new StringBuilder()
        .AppendLine($"内容：{styledText}")
        .AppendLine($"发送ID：`{sendMessageEntity.SendId}`")
        .AppendLine($"发送方：[{sendMessageEntity.SendTitle}](tg://user?id={sendMessageEntity.SendId})   {sendMessageEntity.SendUserNames.JoinUsernames()}")
        .AppendLine($"来源：`{sendMessageEntity.FromTitle}`    {sendMessageEntity.FromUserNames.JoinUsernames()}")
        .AppendLine($"时间：`{message.Date.AddHours(8):yyyy-MM-dd HH:mm:ss}`")
        .AppendLine($"链接：[【直达】](https://t.me/{sendMessageEntity.FromMainUserName ?? $"c/{sendMessageEntity.FromId}"}/{message.id})")
        .AppendLine($"*命中关键词：* {keywordList}")
        .AppendLine("`--------------------------------`");
        return sb.ToString();
    }

    private static KeywordConfig MergeKeywordStyles(IEnumerable<KeywordConfig> list)
    {
        var merged = new KeywordConfig();
        foreach (var k in list)
        {
            merged.IsBold |= k.IsBold;
            merged.IsItalic |= k.IsItalic;
            merged.IsUnderline |= k.IsUnderline;
            merged.IsStrikeThrough |= k.IsStrikeThrough;
            merged.IsQuote |= k.IsQuote;
            merged.IsMonospace |= k.IsMonospace;
            merged.IsSpoiler |= k.IsSpoiler;
        }
        return merged;
    }

    private static string ApplyStylesToText(string text, KeywordConfig cfg)
    {
        var result = text ?? string.Empty;
        if (cfg.IsMonospace)
        {
            result = "`" + result.Replace("`", "\\`") + "`";
        }
        else
        {
            if (cfg.IsBold) result = $"*{result}*";

            if (cfg.IsItalic && cfg.IsUnderline)
            {
                result = $"___{result}_**__";
            }
            else
            {
                if (cfg.IsItalic) result = $"_{result}_";
                if (cfg.IsUnderline) result = $"__{result}__";
            }

            if (cfg.IsStrikeThrough) result = $"~{result}~";
            if (cfg.IsSpoiler) result = $"||{result}||";
        }
        if (cfg.IsQuote)
            result = "\n>" + result.Replace("\n", "\n> ");

        return result;
    }

    private static readonly Regex _phoneRegex = new(@"^\+\d{6,15}$", RegexOptions.Compiled);

    public static bool IsE164Phone(this string? phone)
        => !string.IsNullOrWhiteSpace(phone) && _phoneRegex.IsMatch(phone);
}