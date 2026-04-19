namespace TelegramMonitor;

public interface ITelegramAccountRepository
{
    Task<List<TelegramAccount>> ListAccountsAsync();
    Task<TelegramAccount?> FindByIdAsync(int id);
    Task<TelegramAccount?> FindByPhoneAsync(string phone);
    Task<List<TelegramAccount>> ListMonitoringAccountsAsync();
    Task<TelegramAccount> SaveAsync(TelegramAccount account);
    Task DeleteAsync(int id);
    Task<TelegramAccount> SetMonitoringEnabledAsync(int id, bool enabled);
    Task UpdateLastErrorAsync(int id, string? error);
    Task MarkAuthorizedAsync(int id, User user);
    Task MarkMonitoringStartedAsync(int id);
}
