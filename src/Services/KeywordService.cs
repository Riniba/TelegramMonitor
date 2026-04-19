namespace TelegramMonitor;

public class KeywordService : IKeywordService
{
    private readonly IKeywordRepository _keywordRepository;

    public KeywordService(IKeywordRepository keywordRepository)
    {
        _keywordRepository = keywordRepository;
    }

    public async Task<IReadOnlyList<KeywordRuleDto>> ListAsync(int? accountId = null)
    {
        var entities = await _keywordRepository.ListAsync(accountId);
        return entities.Select(ToDto).ToList();
    }

    public async Task AddAsync(KeywordRuleUpsertRequest request) =>
        await _keywordRepository.AddAsync(ToEntity(request));

    public async Task BatchAddAsync(List<KeywordRuleUpsertRequest> requests)
    {
        if (requests == null || requests.Count == 0)
            throw Oops.Oh("关键词规则不能为空");

        var entities = requests.Select(ToEntity).ToList();
        await _keywordRepository.BatchAddAsync(entities);
    }

    public async Task UpdateAsync(KeywordRuleUpsertRequest request)
    {
        if (request.Id <= 0)
            throw Oops.Oh("关键词规则 ID 无效");

        await _keywordRepository.UpdateAsync(ToEntity(request));
    }

    public Task DeleteAsync(int id) =>
        _keywordRepository.DeleteAsync(id);

    public Task BatchDeleteAsync(IEnumerable<int> ids) =>
        _keywordRepository.BatchDeleteAsync(ids);

    private static KeywordConfig ToEntity(KeywordRuleUpsertRequest request)
    {
        if (request == null)
            throw Oops.Oh("关键词规则不能为空");

        return new KeywordConfig
        {
            Id = request.Id,
            AccountId = request.AccountId,
            RuleName = request.RuleName?.Trim(),
            KeywordPattern = KeywordPatternBuilder.BuildTextPattern(request.KeywordValue, request.MatchMode),
            MatchMode = request.MatchMode,
            IsMatchUser = request.IsMatchUser,
            UserPattern = request.IsMatchUser
                ? KeywordPatternBuilder.BuildUserPattern(request.UserValue ?? string.Empty)
                : null,
            KeywordAction = request.KeywordAction,
            IsCaseSensitive = request.IsCaseSensitive,
            IsEnabled = request.IsEnabled,
            Priority = request.Priority,
            Remark = request.Remark?.Trim()
        };
    }

    private static KeywordRuleDto ToDto(KeywordConfig entity) =>
        new()
        {
            Id = entity.Id,
            AccountId = entity.AccountId,
            RuleName = entity.RuleName,
            KeywordPattern = entity.KeywordPattern,
            MatchMode = entity.MatchMode,
            IsMatchUser = entity.IsMatchUser,
            UserPattern = entity.UserPattern,
            KeywordAction = entity.KeywordAction,
            IsCaseSensitive = entity.IsCaseSensitive,
            IsEnabled = entity.IsEnabled,
            Priority = entity.Priority,
            Remark = entity.Remark,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
}
