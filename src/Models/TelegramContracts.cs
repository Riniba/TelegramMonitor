namespace TelegramMonitor;

public record TelegramStartLoginRequest(
    string PhoneNumber,
    string? Remark = null);

public record TelegramSubmitCodeRequest(int AccountId, string Code);

public record TelegramSubmitPasswordRequest(int AccountId, string Password);

public record TelegramMessageQueryRequest(
    int? AccountId = null,
    long? ChatId = null,
    long? SenderId = null,
    string? Keyword = null,
    bool? IsOutgoing = null,
    bool? IsEdited = null,
    bool? IsChannelPost = null,
    int Page = 1,
    int PageSize = 50);

public record TelegramRuntimeSnapshot(
    int AccountId,
    bool Connected,
    bool Monitoring,
    DateTime? LastMessageAt);

public record TelegramLoginResult(
    int AccountId,
    LoginState State,
    string Message,
    TelegramAccountDto? Account = null);

public record TelegramMonitorOperationResult(
    int AccountId,
    bool Success,
    string Message);

public record PagedResult<T>(
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<T> Items);

public class TelegramAccountDto
{
    public int Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public long? UserId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Remark { get; set; }
    public bool IsEnabled { get; set; }
    public bool MonitorEnabled { get; set; }
    public bool Connected { get; set; }
    public bool Monitoring { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastMonitorStartedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string? LastError { get; set; }
}
