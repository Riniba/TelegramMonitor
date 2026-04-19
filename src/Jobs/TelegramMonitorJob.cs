using Furion.Shapeless;

namespace TelegramMonitor;

[JobDetail("telegram-monitor-job", Description = "保持已启用的 Telegram 监控在线", GroupName = "monitor", Concurrent = true)]
[PeriodSeconds(60, TriggerId = "telegram-monitor-trigger", Description = "定时确保已启用账号持续监控", RunOnStart = true)]
public class TelegramMonitorJob : IJob
{
    private readonly ITelegramAccountRepository _accountRepository;
    private readonly ITelegramAccountRuntimeHub _runtimeHub;
    private readonly ILogger<TelegramMonitorJob> _logger;

    public TelegramMonitorJob(
        ITelegramAccountRepository accountRepository,
        ITelegramAccountRuntimeHub runtimeHub,
        ILogger<TelegramMonitorJob> logger)
    {
        _accountRepository = accountRepository;
        _runtimeHub = runtimeHub;
        _logger = logger;
    }

    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        var accounts = await _accountRepository.ListMonitoringAccountsAsync();
        foreach (var account in accounts)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                var result = await _runtimeHub.StartMonitoringAsync(account);
                if (!result.Success)
                    _logger.LogWarning("账号 {AccountId} 的监控保活失败: {Message}", account.Id, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "账号 {AccountId} 的监控保活执行异常", account.Id);
            }
        }

        // 定时持久化更新状态，避免进程异常退出时丢失过多状态
        _runtimeHub.SaveAllState();
    }
}
