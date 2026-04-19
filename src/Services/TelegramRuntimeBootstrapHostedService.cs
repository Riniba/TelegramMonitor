namespace TelegramMonitor;

public class TelegramRuntimeBootstrapHostedService : BackgroundService
{
    private readonly ITelegramAccountRepository _accountRepository;
    private readonly ITelegramAccountRuntimeHub _runtimeHub;
    private readonly ILogger<TelegramRuntimeBootstrapHostedService> _logger;

    public TelegramRuntimeBootstrapHostedService(
        ITelegramAccountRepository accountRepository,
        ITelegramAccountRuntimeHub runtimeHub,
        ILogger<TelegramRuntimeBootstrapHostedService> logger)
    {
        _accountRepository = accountRepository;
        _runtimeHub = runtimeHub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var accounts = await _accountRepository.ListMonitoringAccountsAsync();
        if (accounts.Count == 0)
        {
            _logger.LogInformation("未发现需要自动恢复监听的账号");
            return;
        }

        _logger.LogInformation("开始自动恢复监听，账号数: {Count}", accounts.Count);

        foreach (var account in accounts)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            var accountLabel = $"ID={account.Id}, 手机号={account.Phone}, 用户名={account.Username ?? "-"}, 备注={account.Remark ?? "-"}";

            try
            {
                var result = await _runtimeHub.StartMonitoringAsync(account);
                if (result.Success)
                {
                    _logger.LogInformation("自动恢复监听成功: {AccountLabel}", accountLabel);
                }
                else
                {
                    _logger.LogWarning("自动恢复监听失败: {AccountLabel}, 原因: {Message}", accountLabel, result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "自动恢复监听异常: {AccountLabel}", accountLabel);
            }
        }
    }
}
