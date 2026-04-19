namespace TelegramMonitor;

public interface IKeywordRepository
{
    Task<List<KeywordConfig>> ListAsync(int? accountId = null);
    Task AddAsync(KeywordConfig keyword);
    Task BatchAddAsync(List<KeywordConfig> keywords);
    Task UpdateAsync(KeywordConfig keyword);
    Task DeleteAsync(int id);
    Task BatchDeleteAsync(IEnumerable<int> ids);
}
