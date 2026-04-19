namespace TelegramMonitor;

[SugarTable("BotNotifyTarget", "Bot通知目标表")]
public class BotNotifyTarget
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键ID")]
    public int Id { get; set; }

    [SugarColumn(ColumnDescription = "Telegram 会话 ID")]
    public long ChatId { get; set; }

    [SugarColumn(Length = 512, ColumnDescription = "会话标题")]
    public string ChatTitle { get; set; } = string.Empty;

    [SugarColumn(IsNullable = true, Length = 256, ColumnDescription = "会话用户名")]
    public string? ChatUsername { get; set; }

    [SugarColumn(Length = 64, ColumnDescription = "会话类型 (Channel/Group/User)")]
    public string ChatType { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    [SugarColumn(IsNullable = true, Length = 256, ColumnDescription = "备注")]
    public string? Remark { get; set; }

    [SugarColumn(ColumnDescription = "创建时间(北京时间)")]
    public DateTime CreatedAt { get; set; } = ChinaTime.Now;

    [SugarColumn(ColumnDescription = "更新时间(北京时间)")]
    public DateTime UpdatedAt { get; set; } = ChinaTime.Now;
}
