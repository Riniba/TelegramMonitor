namespace TelegramMonitor;

public static class KeywordMatchExtensions
{
    public static List<KeywordConfig> Match(
        string? message,
        long? userId,
        IReadOnlyCollection<string>? userNames,
        IEnumerable<KeywordConfig>? allKeywords)
    {
        if (allKeywords == null)
            return new();

        var userCandidates = KeywordPatternBuilder.BuildUserCandidates(userId, userNames);
        var text = message ?? string.Empty;

        return GetActiveRules(allKeywords)
            .Where(rule => IsTextMatch(rule, text))
            .Where(rule => IsUserMatch(rule, userCandidates))
            .ToList();
    }

    private static IEnumerable<KeywordConfig> GetActiveRules(IEnumerable<KeywordConfig> allKeywords) =>
        allKeywords
            .Where(rule => rule != null)
            .Where(rule => rule.IsEnabled)
            .Where(rule => !string.IsNullOrWhiteSpace(rule.KeywordPattern))
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.Id);

    private static bool IsUserMatch(KeywordConfig rule, IReadOnlyCollection<string> userCandidates)
    {
        if (!rule.IsMatchUser)
            return true;

        if (string.IsNullOrWhiteSpace(rule.UserPattern) || userCandidates.Count == 0)
            return false;

        return userCandidates.Any(candidate =>
            KeywordPatternBuilder.IsRegexMatch(candidate, rule.UserPattern, rule.IsCaseSensitive));
    }

    private static bool IsTextMatch(KeywordConfig rule, string message) =>
        KeywordPatternBuilder.IsRegexMatch(message, rule.KeywordPattern, rule.IsCaseSensitive);
}
