namespace TelegramMonitor;

[JobDetail("bot-notify-send-job", Description = "定时发送 Bot 通知队列", GroupName = "bot", Concurrent = false)]
[PeriodSeconds(5, TriggerId = "bot-notify-send-trigger", Description = "每5秒检查并发送待发通知", RunOnStart = false)]
public class BotNotifySendJob : IJob
{
    private const int MaxRetry = 3;
    private const int BatchSize = 50;

    private readonly BotNotifyChannel _channel;
    private readonly IBotService _botService;
    private readonly ILogger<BotNotifySendJob> _logger;

    public BotNotifySendJob(
        BotNotifyChannel channel,
        IBotService botService,
        ILogger<BotNotifySendJob> logger)
    {
        _channel = channel;
        _botService = botService;
        _logger = logger;
    }

    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        if (!_botService.IsEnabled)
            return;

        var sent = 0;
        while (sent < BatchSize && _channel.Reader.TryRead(out var item))
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            await SendWithRetryAsync(item);
            sent++;
        }
    }

    private async Task SendWithRetryAsync(BotNotifyItem item)
    {
        for (var attempt = 1; attempt <= MaxRetry; attempt++)
        {
            try
            {
                var sentMessageId = await _botService.SendNotifyMessageAsync(
                    item.TargetChatId,
                    item.MessageText,
                    item.CallbackData);

                _logger.LogDebug("通知已发送到 {ChatTitle}({ChatId}), MessageId={SentMessageId}",
                    item.TargetChatTitle, item.TargetChatId, sentMessageId);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "通知发送到 {ChatTitle}({ChatId}) 失败 (第{Retry}次)",
                    item.TargetChatTitle, item.TargetChatId, attempt);

                if (attempt < MaxRetry)
                    await Task.Delay(1000 * attempt, CancellationToken.None);
            }
        }
    }
}
