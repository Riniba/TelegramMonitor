namespace TelegramMonitor;

public class TelegramAccountRepository : ITelegramAccountRepository
{
    private readonly ISqlSugarClient _db;

    public TelegramAccountRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task<List<TelegramAccount>> ListAccountsAsync() =>
        _db.Queryable<TelegramAccount>()
            .OrderByDescending(x => x.Id)
            .ToListAsync();

    public Task<TelegramAccount?> FindByIdAsync(int id) =>
        _db.Queryable<TelegramAccount>()
            .FirstAsync(x => x.Id == id);

    public Task<TelegramAccount?> FindByPhoneAsync(string phone) =>
        _db.Queryable<TelegramAccount>()
            .FirstAsync(x => x.Phone == phone);

    public Task<List<TelegramAccount>> ListMonitoringAccountsAsync() =>
        _db.Queryable<TelegramAccount>()
            .Where(x => x.IsEnabled && x.MonitorEnabled)
            .OrderBy(x => x.Id)
            .ToListAsync();

    public async Task<TelegramAccount> SaveAsync(TelegramAccount account)
    {
        account.UpdatedAt = ChinaTime.Now;

        if (account.Id == 0)
        {
            account.CreatedAt = account.UpdatedAt;
            account.Id = await _db.Insertable(account).ExecuteReturnIdentityAsync();
            return account;
        }

        await _db.Updateable(account).ExecuteCommandAsync();
        return account;
    }

    public Task DeleteAsync(int id) =>
        _db.Deleteable<TelegramAccount>()
            .In(id)
            .ExecuteCommandAsync();

    public async Task<TelegramAccount> SetMonitoringEnabledAsync(int id, bool enabled)
    {
        var account = await RequireAsync(id);
        account.MonitorEnabled = enabled;
        account.LastError = null;
        await SaveAsync(account);
        return account;
    }

    public async Task UpdateLastErrorAsync(int id, string? error)
    {
        var account = await FindByIdAsync(id);
        if (account == null)
            return;

        account.LastError = string.IsNullOrWhiteSpace(error) ? null : error.Trim();
        if (string.IsNullOrWhiteSpace(error))
            account.LastSeenAt = ChinaTime.Now;

        await SaveAsync(account);
    }

    public async Task MarkAuthorizedAsync(int id, User user)
    {
        var account = await RequireAsync(id);
        account.UserId = user.ID;
        account.Phone = (user.phone ?? account.Phone).NormalizeTelegramPhone();
        account.Username = user.MainUsername;
        account.FirstName = user.first_name;
        account.LastName = user.last_name;
        account.LastLoginAt = ChinaTime.Now;
        account.LastSeenAt = ChinaTime.Now;
        account.LastError = null;
        await SaveAsync(account);
    }

    public async Task MarkMonitoringStartedAsync(int id)
    {
        var account = await FindByIdAsync(id);
        if (account == null)
            return;

        account.LastMonitorStartedAt = ChinaTime.Now;
        account.LastSeenAt = ChinaTime.Now;
        account.LastError = null;
        await SaveAsync(account);
    }

    private async Task<TelegramAccount> RequireAsync(int id) =>
        await FindByIdAsync(id) ?? throw Oops.Oh($"账号不存在: {id}");
}
