namespace TelegramMonitor;

[SugarTable("TelegramAccount", "Telegram账号表")]
public class TelegramAccount
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键ID")]
    public int Id { get; set; }

    [SugarColumn(Length = 32, ColumnDescription = "手机号")]
    public string Phone { get; set; } = string.Empty;

    [SugarColumn(IsNullable = true, ColumnDescription = "Telegram用户ID")]
    public long? UserId { get; set; }

    [SugarColumn(IsNullable = true, Length = 128, ColumnDescription = "用户名")]
    public string Username { get; set; }

    [SugarColumn(IsNullable = true, Length = 256, ColumnDescription = "名字")]
    public string FirstName { get; set; }

    [SugarColumn(IsNullable = true, Length = 256, ColumnDescription = "姓氏")]
    public string LastName { get; set; }

    [SugarColumn(IsNullable = true, Length = 256, ColumnDescription = "备注")]
    public string Remark { get; set; }

    [SugarColumn(Length = 512, ColumnDescription = "会话文件路径")]
    public string SessionPath { get; set; } = string.Empty;

    [SugarColumn(Length = 512, ColumnDescription = "更新状态文件路径")]
    public string StatePath { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "Telegram ApiId")]
    public int ApiId { get; set; }

    [SugarColumn(Length = 128, ColumnDescription = "Telegram ApiHash")]
    public string ApiHash { get; set; } = string.Empty;

    [SugarColumn(IsNullable = true, Length = 128, ColumnDescription = "会话密钥")]
    public string SessionKey { get; set; }

    [SugarColumn(ColumnDescription = "代理类型")]
    public ProxyType ProxyType { get; set; } = ProxyType.None;

    [SugarColumn(IsNullable = true, Length = 256, ColumnDescription = "代理服务器")]
    public string ProxyServer { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "代理端口")]
    public int? ProxyPort { get; set; }

    [SugarColumn(IsNullable = true, Length = 256, ColumnDescription = "代理用户名")]
    public string ProxyUsername { get; set; }

    [SugarColumn(IsNullable = true, Length = 256, ColumnDescription = "代理密码")]
    public string ProxyPassword { get; set; }

    [SugarColumn(IsNullable = true, Length = 256, ColumnDescription = "代理密钥")]
    public string ProxySecret { get; set; }

    [SugarColumn(IsNullable = true, Length = 256, ColumnDescription = "二步验证密码")]
    public string TwoFactorPassword { get; set; }

    [SugarColumn(ColumnDescription = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    [SugarColumn(ColumnDescription = "是否开启监控")]
    public bool MonitorEnabled { get; set; }

    [SugarColumn(ColumnDescription = "创建时间(北京时间)")]
    public DateTime CreatedAt { get; set; } = ChinaTime.Now;

    [SugarColumn(ColumnDescription = "更新时间(北京时间)")]
    public DateTime UpdatedAt { get; set; } = ChinaTime.Now;

    [SugarColumn(IsNullable = true, ColumnDescription = "最后登录时间(北京时间)")]
    public DateTime? LastLoginAt { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "最后启动监控时间(北京时间)")]
    public DateTime? LastMonitorStartedAt { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "最后活跃时间(北京时间)")]
    public DateTime? LastSeenAt { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "最后错误信息")]
    public string LastError { get; set; }
}
