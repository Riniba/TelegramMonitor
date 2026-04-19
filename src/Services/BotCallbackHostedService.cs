namespace TelegramMonitor;

public class BotCallbackHostedService : IHostedService
{
    private readonly IBotService _botService;
    private readonly ILogger<BotCallbackHostedService> _logger;

    public BotCallbackHostedService(
        IBotService botService,
        ILogger<BotCallbackHostedService> logger)
    {
        _botService = botService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bot 托管服务启动中...");
        await _botService.InitializeAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bot 托管服务停止中...");
        if (_botService is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
    }
}
