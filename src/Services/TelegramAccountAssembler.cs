namespace TelegramMonitor;

public static class TelegramAccountAssembler
{
    public static TelegramAccountDto ToDto(
        TelegramAccount account,
        IEnumerable<TelegramRuntimeSnapshot>? runtimeSnapshots = null)
    {
        var runtime = runtimeSnapshots?.FirstOrDefault(x => x.AccountId == account.Id);
        var displayName = string.Join(" ", new[] { account.FirstName, account.LastName }
            .Where(x => !string.IsNullOrWhiteSpace(x)))
            .Trim();
        var resolvedDisplayName = !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : account.Username ?? account.Phone;

        return new TelegramAccountDto
        {
            Id = account.Id,
            Phone = account.Phone,
            UserId = account.UserId,
            Username = account.Username,
            FirstName = account.FirstName,
            LastName = account.LastName,
            DisplayName = resolvedDisplayName,
            Remark = account.Remark,
            IsEnabled = account.IsEnabled,
            MonitorEnabled = account.MonitorEnabled,
            Connected = runtime?.Connected == true,
            Monitoring = runtime?.Monitoring == true,
            LastLoginAt = account.LastLoginAt,
            LastMonitorStartedAt = account.LastMonitorStartedAt,
            LastSeenAt = account.LastSeenAt,
            LastError = account.LastError
        };
    }
}
