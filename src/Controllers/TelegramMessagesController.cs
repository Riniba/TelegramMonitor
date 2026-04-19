namespace TelegramMonitor;

[ApiController]
[Route("api")]
[ApiDescriptionSettings(Tag = "telegram.messages", Description = "Telegram 消息接口")]
[Authorize]
public class TelegramMessagesController : ControllerBase
{
    private readonly ITelegramMessageArchiveService _messageArchive;

    public TelegramMessagesController(ITelegramMessageArchiveService messageArchive)
    {
        _messageArchive = messageArchive;
    }

    [HttpGet("messages")]
    public Task<PagedResult<TelegramMessageRecord>> PageAsync([FromQuery] TelegramMessageQueryRequest request) =>
        _messageArchive.QueryPageAsync(request);

    [HttpGet("messages/{id}")]
    public async Task<TelegramMessageRecord> GetAsync(int id) =>
        await _messageArchive.FindByIdAsync(id) ?? throw Oops.Oh($"消息不存在: {id}");
}
