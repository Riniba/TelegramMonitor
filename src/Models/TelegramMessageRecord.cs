namespace TelegramMonitor;

[SugarTable("TelegramMessageRecord", "Telegram消息归档表")]
public class TelegramMessageRecord
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键ID")]
    public int Id { get; set; }

    [SugarColumn(ColumnDescription = "所属账号ID")]
    public int AccountId { get; set; }

    [SugarColumn(Length = 64, ColumnDescription = "更新类型")]
    public string UpdateType { get; set; } = string.Empty;

    [SugarColumn(Length = 64, ColumnDescription = "消息对象类型")]
    public string MessageType { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "Telegram消息ID")]
    public int TelegramMessageId { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "会话ID")]
    public long? ChatId { get; set; }

    [SugarColumn(IsNullable = true, Length = 64, ColumnDescription = "会话类型")]
    public string? ChatType { get; set; }

    [SugarColumn(IsNullable = true, Length = 512, ColumnDescription = "会话标题")]
    public string? ChatTitle { get; set; }

    [SugarColumn(ColumnName = "ChatUserName", IsNullable = true, Length = 256, ColumnDescription = "会话用户名")]
    public string? ChatUsername { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "发送者ID")]
    public long? SenderId { get; set; }

    [SugarColumn(IsNullable = true, Length = 512, ColumnDescription = "发送者标题")]
    public string? SenderTitle { get; set; }

    [SugarColumn(ColumnName = "SenderUserName", IsNullable = true, Length = 256, ColumnDescription = "发送者用户名")]
    public string? SenderUsername { get; set; }

    [SugarColumn(ColumnDescription = "是否自己发出")]
    public bool IsOutgoing { get; set; }

    [SugarColumn(ColumnDescription = "是否编辑消息")]
    public bool IsEdited { get; set; }

    [SugarColumn(ColumnDescription = "是否频道帖子")]
    public bool IsChannelPost { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "消息时间(UTC+8)")]
    public DateTime? MessageDate { get; set; }

    [SugarColumn(ColumnDescription = "接收时间(UTC+8)")]
    public DateTime ReceivedAt { get; set; } = ChinaTime.Now;

    [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "消息文本")]
    public string? Text { get; set; }

    [SugarColumn(IsNullable = true, Length = 128, ColumnDescription = "媒体类型")]
    public string? MediaType { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "回复消息ID")]
    public int? ReplyToMessageId { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "话题线程ID")]
    public int? ThreadId { get; set; }

    [SugarColumn(IsNullable = true, Length = 128, ColumnDescription = "服务消息动作类型")]
    public string? ServiceActionType { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "原始JSON快照")]
    public string? RawJson { get; set; }
}
