namespace TelegramMonitor;

public class KeywordRuleUpsertRequest
{
    public int Id { get; set; }
    public int? AccountId { get; set; }
    public string? RuleName { get; set; }
    public string KeywordValue { get; set; } = string.Empty;
    public KeywordMatchMode MatchMode { get; set; } = KeywordMatchMode.Contains;
    public bool IsMatchUser { get; set; }
    public string? UserValue { get; set; }
    public KeywordAction KeywordAction { get; set; } = KeywordAction.Monitor;
    public bool IsCaseSensitive { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; }
    public string? Remark { get; set; }
}

public class KeywordRuleDto
{
    public int Id { get; set; }
    public int? AccountId { get; set; }
    public string? RuleName { get; set; }
    public string KeywordPattern { get; set; } = string.Empty;
    public KeywordMatchMode MatchMode { get; set; }
    public bool IsMatchUser { get; set; }
    public string? UserPattern { get; set; }
    public KeywordAction KeywordAction { get; set; }
    public bool IsCaseSensitive { get; set; }
    public bool IsEnabled { get; set; }
    public int Priority { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
