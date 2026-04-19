namespace TelegramMonitor;

public class BotNotifyTargetRepository : IBotNotifyTargetRepository
{
    private readonly ISqlSugarClient _db;

    public BotNotifyTargetRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task<List<BotNotifyTarget>> ListAsync() =>
        _db.Queryable<BotNotifyTarget>()
            .OrderBy(x => x.Id)
            .ToListAsync();

    public Task<List<BotNotifyTarget>> ListEnabledAsync() =>
        _db.Queryable<BotNotifyTarget>()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Id)
            .ToListAsync();

    public Task<BotNotifyTarget?> FindByIdAsync(int id) =>
        _db.Queryable<BotNotifyTarget>()
            .FirstAsync(x => x.Id == id);

    public Task<BotNotifyTarget?> FindByChatIdAsync(long chatId) =>
        _db.Queryable<BotNotifyTarget>()
            .FirstAsync(x => x.ChatId == chatId);

    public async Task<BotNotifyTarget> AddAsync(BotNotifyTarget target)
    {
        target.CreatedAt = ChinaTime.Now;
        target.UpdatedAt = target.CreatedAt;
        target.Id = await _db.Insertable(target).ExecuteReturnIdentityAsync();
        return target;
    }

    public async Task SetEnabledAsync(int id, bool enabled)
    {
        var target = await FindByIdAsync(id);
        if (target == null)
            throw Oops.Oh($"通知目标不存在: {id}");

        target.IsEnabled = enabled;
        target.UpdatedAt = ChinaTime.Now;
        await _db.Updateable(target).ExecuteCommandAsync();
    }

    public async Task DeleteAsync(int id)
    {
        if (id <= 0) return;
        await _db.Deleteable<BotNotifyTarget>().In(id).ExecuteCommandAsync();
    }
}
