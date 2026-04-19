namespace TelegramMonitor;

[SugarTable("KeywordConfig", "关键词规则表")]
public class KeywordConfig
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键ID")]
    [Description("关键词规则 ID")]
    public int Id { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "关联账号 ID（为空表示全局规则）")]
    [Description("关联账号 ID（为空表示全局规则）")]
    public int? AccountId { get; set; }

    [SugarColumn(IsNullable = true, Length = 128, ColumnDescription = "规则名称")]
    [Description("规则名称")]
    public string? RuleName { get; set; }

    [SugarColumn(Length = 1024, ColumnDescription = "文本匹配正则")]
    [Description("文本匹配正则")]
    public string KeywordPattern { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "文本匹配方式")]
    [Description("文本匹配方式")]
    public KeywordMatchMode MatchMode { get; set; } = KeywordMatchMode.Contains;

    [SugarColumn(ColumnDescription = "是否匹配用户")]
    [Description("是否匹配用户")]
    public bool IsMatchUser { get; set; }

    [SugarColumn(IsNullable = true, Length = 1024, ColumnDescription = "用户匹配正则")]
    [Description("用户匹配正则")]
    public string? UserPattern { get; set; }

    [SugarColumn(ColumnName = "KeywordAction", ColumnDescription = "命中动作")]
    [Description("命中动作")]
    public KeywordAction KeywordAction { get; set; } = KeywordAction.Monitor;

    [SugarColumn(ColumnName = "IsCaseSensitive", ColumnDescription = "是否区分大小写")]
    [Description("是否区分大小写")]
    public bool IsCaseSensitive { get; set; }

    [SugarColumn(ColumnDescription = "是否启用")]
    [Description("是否启用")]
    public bool IsEnabled { get; set; } = true;

    [SugarColumn(ColumnDescription = "优先级")]
    [Description("优先级")]
    public int Priority { get; set; }

    [SugarColumn(IsNullable = true, Length = 256, ColumnDescription = "备注")]
    [Description("备注")]
    public string? Remark { get; set; }

    [SugarColumn(ColumnDescription = "创建时间（UTC+8）")]
    [Description("创建时间（UTC+8）")]
    public DateTime CreatedAt { get; set; } = ChinaTime.Now;

    [SugarColumn(ColumnDescription = "更新时间（UTC+8）")]
    [Description("更新时间（UTC+8）")]
    public DateTime UpdatedAt { get; set; } = ChinaTime.Now;
}

public enum KeywordMatchMode
{
    [Description("全部匹配")]
    Exact = 0,

    [Description("包含匹配")]
    Contains = 1,

    [Description("正则匹配")]
    Regex = 2,

    [Description("模糊匹配多个关键词(以?分隔)")]
    Fuzzy = 3
}

public enum KeywordAction
{
    [Description("排除")]
    Exclude = 0,

    [Description("监控")]
    Monitor = 1
}
