namespace TelegramMonitor;

public class SystemCacheServices : ISingleton
{
    private readonly ISqlSugarClient _db;
    private readonly IMemoryCache _cache;

    public SystemCacheServices(IMemoryCache cache, ISqlSugarClient db)
    {
        _cache = cache;
        _db = db;
    }

    public async Task<List<KeywordConfig>> GetKeywordsAsync()
    {
        return await _db.Queryable<KeywordConfig>().ToListAsync();
    }

    public async Task AddKeywordsAsync(KeywordConfig keyword)
    {
        var list = await GetKeywordsAsync();
        if (list.Any(k => k.KeywordType == keyword.KeywordType &&
                          k.KeywordContent.Equals(keyword.KeywordContent, StringComparison.OrdinalIgnoreCase)))
        {
            throw Oops.Oh("关键词已存在，请勿重复添加");
        }

        keyword.Id = await _db.Insertable(keyword).ExecuteReturnIdentityAsync();
    }

    public async Task BatchAddKeywordsAsync(List<KeywordConfig> keywords)
    {
        var list = await GetKeywordsAsync();

        var toAdd = keywords
            .Where(k => !list.Any(e => e.KeywordType == k.KeywordType &&
                                       e.KeywordContent.Equals(k.KeywordContent, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (toAdd.Count == 0)
            throw Oops.Oh("所有关键词已存在，无需重复添加");

        await _db.Insertable(toAdd).ExecuteCommandAsync();
    }

    public async Task UpdateKeywordsAsync(KeywordConfig keyword)
    {
        var list = await GetKeywordsAsync();
        if (list.Any(k => k.Id != keyword.Id &&
                          k.KeywordType == keyword.KeywordType &&
                          k.KeywordContent.Equals(keyword.KeywordContent, StringComparison.OrdinalIgnoreCase)))
        {
            throw Oops.Oh("关键词已存在，请修改后重试");
        }

        await _db.Updateable(keyword).ExecuteCommandAsync();
    }

    public async Task DeleteKeywordsAsync(int id)
    {
        await _db.Deleteable<KeywordConfig>().In(id).ExecuteCommandAsync();
    }

    public async Task BatchDeleteKeywordsAsync(IEnumerable<int> ids)
    {
        var idArr = ids.ToArray();
        await _db.Deleteable<KeywordConfig>().In(idArr).ExecuteCommandAsync();
    }

    
}