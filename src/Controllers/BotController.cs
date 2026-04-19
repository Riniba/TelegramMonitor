namespace TelegramMonitor;

[ApiController]
[Route("api")]
[ApiDescriptionSettings(Tag = "bot", Description = "Bot 通知接口")]
[Authorize]
public class BotController : ControllerBase
{
    private readonly IBotService _botService;
    private readonly IBotNotifyTargetRepository _repository;

    public BotController(IBotService botService, IBotNotifyTargetRepository repository)
    {
        _botService = botService;
        _repository = repository;
    }

    [HttpGet("bot/status")]
    public async Task<BotStatusDto> GetStatusAsync() =>
        await _botService.GetStatusAsync();

    [HttpGet("bot/targets")]
    public async Task<List<BotNotifyTargetDto>> ListTargetsAsync()
    {
        var list = await _repository.ListAsync();
        return list.Select(t => new BotNotifyTargetDto(
            t.Id, t.ChatId, t.ChatTitle, t.ChatUsername,
            t.ChatType, t.IsEnabled, t.Remark, t.CreatedAt)).ToList();
    }

    [HttpPost("bot/targets")]
    public async Task<BotNotifyTargetDto> AddTargetAsync([FromBody] BotNotifyTargetAddRequest request)
    {
        var target = await _botService.ValidateAndAddTargetAsync(request.ChatIdentifier, request.Remark);
        return new BotNotifyTargetDto(
            target.Id, target.ChatId, target.ChatTitle, target.ChatUsername,
            target.ChatType, target.IsEnabled, target.Remark, target.CreatedAt);
    }

    [HttpPut("bot/targets/{id}/toggle")]
    public async Task ToggleTargetAsync(int id, [FromQuery] bool enabled) =>
        await _repository.SetEnabledAsync(id, enabled);

    [HttpDelete("bot/targets/{id}")]
    public async Task DeleteTargetAsync(int id) =>
        await _repository.DeleteAsync(id);
}
