namespace TelegramMonitor;

public record BotNotifyTargetAddRequest(string ChatIdentifier, string? Remark = null);

public record BotNotifyTargetDto(
    int Id,
    long ChatId,
    string ChatTitle,
    string? ChatUsername,
    string ChatType,
    bool IsEnabled,
    string? Remark,
    DateTime CreatedAt);

public record BotStatusDto(
    bool Enabled,
    int BotCount,
    int ConnectedCount,
    List<string> BotUsernames);
