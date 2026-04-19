namespace TelegramMonitor;

public interface IBotService
{
    bool IsEnabled { get; }
    Task InitializeAsync();
    Task<BotStatusDto> GetStatusAsync();
    Task<BotNotifyTarget> ValidateAndAddTargetAsync(string chatIdentifier, string? remark);
    Task<int> SendNotifyMessageAsync(long chatId, string htmlText, string? callbackData);
}
