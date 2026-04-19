namespace TelegramMonitor;

[ApiController]
[Route("api")]
[ApiDescriptionSettings(Tag = "keywords", Description = "关键词接口")]
[Authorize]
public class KeywordsController : ControllerBase
{
    private readonly IKeywordService _keywordService;

    public KeywordsController(IKeywordService keywordService)
    {
        _keywordService = keywordService;
    }

    [HttpGet("keywords")]
    public async Task<IReadOnlyList<KeywordRuleDto>> ListAsync([FromQuery] int? accountId = null) =>
        await _keywordService.ListAsync(accountId);

    [HttpPost("keywords")]
    public async Task AddAsync([FromBody] KeywordRuleUpsertRequest keyword) =>
        await _keywordService.AddAsync(keyword);

    [HttpPost("keywords/batch")]
    public async Task BatchAddAsync([FromBody] List<KeywordRuleUpsertRequest> keywords) =>
        await _keywordService.BatchAddAsync(keywords);

    [HttpPut("keywords")]
    public async Task UpdateAsync([FromBody] KeywordRuleUpsertRequest keyword) =>
        await _keywordService.UpdateAsync(keyword);

    [HttpDelete("keywords/{id}")]
    public async Task DeleteAsync(int id) =>
        await _keywordService.DeleteAsync(id);

    [HttpPost("keywords/batch-delete")]
    public async Task BatchDeleteAsync([FromBody] IEnumerable<int> ids) =>
        await _keywordService.BatchDeleteAsync(ids);
}
