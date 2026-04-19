namespace TelegramMonitor;

public static class KeywordPatternBuilder
{
    public const string MatchAllPattern = "^.*$";

    public static string BuildTextPattern(string source, KeywordMatchMode mode)
    {
        var value = RequireValue(source, "关键词内容");

        var pattern = mode switch
        {
            KeywordMatchMode.Exact => $"^{Regex.Escape(value)}$",
            KeywordMatchMode.Contains => Regex.Escape(value),
            KeywordMatchMode.Regex => value,
            KeywordMatchMode.Fuzzy => BuildFuzzyPattern(value),
            _ => throw Oops.Oh("关键词匹配方式无效")
        };

        EnsureValidRegex(pattern, "关键词正则");
        return pattern;
    }

    public static string BuildUserPattern(string source)
    {
        var tokens = SplitTokens(source)
            .Select(RemoveAtPrefix)
            .ToArray();

        if (tokens.Length == 0)
            throw Oops.Oh("启用匹配用户时，用户内容不能为空");

        var pattern = $"^(?:{string.Join("|", tokens.Select(Regex.Escape))})$";
        EnsureValidRegex(pattern, "用户匹配正则");
        return pattern;
    }

    public static string BuildFuzzyPattern(string source)
    {
        var parts = source
            .Split('?', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .Select(Regex.Escape)
            .ToArray();

        if (parts.Length == 0)
            throw Oops.Oh("模糊匹配内容不能为空");

        var lookaheads = string.Concat(parts.Select(part => $"(?=.*{part})"));
        var pattern = $"^{lookaheads}.*$";
        EnsureValidRegex(pattern, "关键词正则");
        return pattern;
    }

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    public static void EnsureValidRegex(string pattern, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw Oops.Oh($"{fieldName}不能为空");

        try
        {
            _ = new Regex(pattern, RegexOptions.CultureInvariant, RegexTimeout);
        }
        catch (ArgumentException ex)
        {
            throw Oops.Oh($"{fieldName}无效: {ex.Message}");
        }
    }

    public static bool IsRegexMatch(string input, string pattern, bool caseSensitive)
    {
        try
        {
            var options = RegexOptions.CultureInvariant;
            if (!caseSensitive)
                options |= RegexOptions.IgnoreCase;

            return Regex.IsMatch(input ?? string.Empty, pattern, options, RegexTimeout);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    public static IReadOnlyList<string> BuildUserCandidates(long? userId, IEnumerable<string>? userNames)
    {
        var values = new List<string>();

        if (userId.HasValue)
            values.Add(userId.Value.ToString());

        if (userNames != null)
        {
            values.AddRange(userNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(RemoveAtPrefix));
        }

        return values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string RequireValue(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw Oops.Oh($"{fieldName}不能为空");

        return normalized;
    }

    private static string[] SplitTokens(string? value) =>
        (value ?? string.Empty)
            .Split(new[] { '\r', '\n', ',', ';', '|', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToArray();

    private static string RemoveAtPrefix(string value) =>
        value.StartsWith("@", StringComparison.Ordinal) ? value[1..] : value;
}
