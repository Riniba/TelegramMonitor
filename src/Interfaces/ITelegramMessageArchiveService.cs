namespace TelegramMonitor;

public interface ITelegramMessageArchiveService
{
    Task<TelegramMessageRecord> ArchiveAsync(
        int accountId,
        UpdateManager manager,
        string updateType,
        MessageBase messageBase,
        bool isEdited);

    Task<TelegramMessageRecord?> FindByIdAsync(int id);
    Task<PagedResult<TelegramMessageRecord>> QueryPageAsync(TelegramMessageQueryRequest request);
}
