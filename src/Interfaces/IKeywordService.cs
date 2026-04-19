namespace TelegramMonitor;

public interface IKeywordService
{
    Task<IReadOnlyList<KeywordRuleDto>> ListAsync(int? accountId = null);
    Task AddAsync(KeywordRuleUpsertRequest request);
    Task BatchAddAsync(List<KeywordRuleUpsertRequest> requests);
    Task UpdateAsync(KeywordRuleUpsertRequest request);
    Task DeleteAsync(int id);
    Task BatchDeleteAsync(IEnumerable<int> ids);
}
