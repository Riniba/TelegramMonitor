namespace TelegramMonitor;

public interface IBotNotifyTargetRepository
{
    Task<List<BotNotifyTarget>> ListAsync();
    Task<List<BotNotifyTarget>> ListEnabledAsync();
    Task<BotNotifyTarget?> FindByIdAsync(int id);
    Task<BotNotifyTarget?> FindByChatIdAsync(long chatId);
    Task<BotNotifyTarget> AddAsync(BotNotifyTarget target);
    Task SetEnabledAsync(int id, bool enabled);
    Task DeleteAsync(int id);
}
