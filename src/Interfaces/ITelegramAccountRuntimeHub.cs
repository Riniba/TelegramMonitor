namespace TelegramMonitor;

public interface ITelegramAccountRuntimeHub : IAsyncDisposable
{
    IReadOnlyList<TelegramRuntimeSnapshot> GetRuntimeSnapshots();
    Task<Client> GetOrCreateClientAsync(TelegramAccount account);
    Task<TelegramLoginResult> StartLoginAsync(TelegramAccount account);
    Task<TelegramLoginResult> SubmitCodeAsync(TelegramAccount account, string code);
    Task<TelegramLoginResult> SubmitPasswordAsync(TelegramAccount account, string password);
    Task<(bool Success, User? User, string? Error)> EnsureAuthorizedAsync(TelegramAccount account);
    Task<TelegramMonitorOperationResult> StartMonitoringAsync(TelegramAccount account);
    Task<TelegramMonitorOperationResult> StopMonitoringAsync(int accountId);
    Task RemoveClientAsync(int accountId);
    Task RemoveAllAsync();
    void SaveAllState();
}
