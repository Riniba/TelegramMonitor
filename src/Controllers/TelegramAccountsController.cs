namespace TelegramMonitor;

[ApiController]
[Route("api")]
[ApiDescriptionSettings(Tag = "telegram.accounts", Description = "Telegram 账号接口")]
[Authorize]
public class TelegramAccountsController : ControllerBase
{
    private readonly ITelegramAccountRepository _accountRepository;
    private readonly ITelegramAccountRuntimeHub _runtimeHub;
    private readonly IConfiguration _configuration;

    public TelegramAccountsController(
        ITelegramAccountRepository accountRepository,
        ITelegramAccountRuntimeHub runtimeHub,
        IConfiguration configuration)
    {
        _accountRepository = accountRepository;
        _runtimeHub = runtimeHub;
        _configuration = configuration;
    }

    [HttpGet("accounts")]
    public async Task<IReadOnlyList<TelegramAccountDto>> ListAsync()
    {
        var accounts = await _accountRepository.ListAccountsAsync();
        var runtimeSnapshots = _runtimeHub.GetRuntimeSnapshots();
        return accounts.Select(account => TelegramAccountAssembler.ToDto(account, runtimeSnapshots)).ToList();
    }

    [HttpGet("accounts/{id}")]
    public async Task<TelegramAccountDto> DetailAsync(int id)
    {
        var account = await RequireAccountAsync(id);
        return TelegramAccountAssembler.ToDto(account, _runtimeHub.GetRuntimeSnapshots());
    }

    [HttpPost("accounts/login/start")]
    public async Task<TelegramLoginResult> StartLoginAsync([FromBody] TelegramStartLoginRequest request)
    {
        var phone = request.PhoneNumber.NormalizeTelegramPhone();
        if (string.IsNullOrWhiteSpace(phone))
            throw Oops.Oh("手机号格式无效");

        var options = GetTelegramOptions();
        var apiId = options.DefaultApiId;
        var apiHash = options.DefaultApiHash?.Trim();

        if (apiId <= 0 || string.IsNullOrWhiteSpace(apiHash))
            throw Oops.Oh("必须先在配置中设置 Telegram 的 ApiId 和 ApiHash");

        var account = await _accountRepository.FindByPhoneAsync(phone) ?? new TelegramAccount();
        account.Phone = phone;
        account.ApiId = apiId;
        account.ApiHash = apiHash;
        account.SessionKey = apiHash;
        account.SessionPath = BuildSessionPath(phone, options);
        account.StatePath = BuildStatePath(phone, options);
        account.Remark = string.IsNullOrWhiteSpace(request.Remark) ? account.Remark : request.Remark.Trim();
        account.IsEnabled = true;
        await _accountRepository.SaveAsync(account);

        return await _runtimeHub.StartLoginAsync(account);
    }

    [HttpPost("accounts/login/code")]
    public async Task<TelegramLoginResult> SubmitCodeAsync([FromBody] TelegramSubmitCodeRequest request)
    {
        var account = await RequireAccountAsync(request.AccountId);
        return await _runtimeHub.SubmitCodeAsync(account, request.Code);
    }

    [HttpPost("accounts/login/password")]
    public async Task<TelegramLoginResult> SubmitPasswordAsync([FromBody] TelegramSubmitPasswordRequest request)
    {
        var account = await RequireAccountAsync(request.AccountId);
        return await _runtimeHub.SubmitPasswordAsync(account, request.Password);
    }

    [HttpPost("accounts/{id}/monitor/start")]
    public async Task<TelegramMonitorOperationResult> StartMonitorAsync(int id)
    {
        var account = await _accountRepository.SetMonitoringEnabledAsync(id, true);
        return await _runtimeHub.StartMonitoringAsync(account);
    }

    [HttpPost("accounts/{id}/monitor/stop")]
    public async Task<TelegramMonitorOperationResult> StopMonitorAsync(int id)
    {
        await _accountRepository.SetMonitoringEnabledAsync(id, false);
        return await _runtimeHub.StopMonitoringAsync(id);
    }

    [HttpPost("accounts/{id}/reconnect")]
    public async Task<TelegramMonitorOperationResult> ReconnectAsync(int id)
    {
        var account = await RequireAccountAsync(id);
        await _runtimeHub.RemoveClientAsync(id);
        var authorized = await _runtimeHub.EnsureAuthorizedAsync(account);
        if (!authorized.Success)
            return new TelegramMonitorOperationResult(id, false, authorized.Error ?? "重连失败");

        if (account.MonitorEnabled)
            return await _runtimeHub.StartMonitoringAsync(account);

        return new TelegramMonitorOperationResult(id, true, "重连成功");
    }

    [HttpGet("accounts/{id}/dialogs")]
    public async Task<IReadOnlyList<TelegramDialogOption>> DialogsAsync(int id)
    {
        var account = await RequireAccountAsync(id);
        var authorized = await _runtimeHub.EnsureAuthorizedAsync(account);
        if (!authorized.Success)
            throw Oops.Oh(authorized.Error ?? "账号尚未登录");

        var client = await _runtimeHub.GetOrCreateClientAsync(account);
        var dialogs = await client.Messages_GetAllDialogs();

        return dialogs.chats.Values
            .Where(chat => chat.IsActive)
            .Select(chat => new TelegramDialogOption
            {
                Id = chat.ID,
                DisplayTitle = $"[{GetChatType(chat)}]{(string.IsNullOrWhiteSpace(chat.MainUsername) ? "" : $"(@{chat.MainUsername})")}{chat.Title}"
            })
            .OrderBy(x => x.DisplayTitle)
            .ToList();
    }

    [HttpDelete("accounts/{id}")]
    public async Task DeleteAsync(int id, [FromQuery] bool deleteSessionFiles = false)
    {
        var account = await RequireAccountAsync(id);
        await _runtimeHub.RemoveClientAsync(id);
        await _accountRepository.DeleteAsync(id);

        if (!deleteSessionFiles)
            return;

        TryDeleteFile(account.SessionPath);
        TryDeleteFile(account.StatePath);
    }

    private async Task<TelegramAccount> RequireAccountAsync(int id) =>
        await _accountRepository.FindByIdAsync(id) ?? throw Oops.Oh($"账号不存在: {id}");

    private TelegramOptions GetTelegramOptions() =>
        _configuration.GetSection("Telegram").Get<TelegramOptions>() ?? new TelegramOptions();

    private static string BuildSessionPath(string phone, TelegramOptions options)
    {
        var root = Path.GetFullPath(options.SessionsPath);
        Directory.CreateDirectory(root);
        return Path.Combine(root, $"{phone}.session");
    }

    private static string BuildStatePath(string phone, TelegramOptions options)
    {
        var root = Path.GetFullPath(options.SessionsPath);
        Directory.CreateDirectory(root);
        return Path.Combine(root, $"{phone}.state.json");
    }

    private static void TryDeleteFile(string? path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
        catch
        {
        }
    }

    private static string GetChatType(ChatBase chat) => chat switch
    {
        TL.Chat => "普通群",
        TL.Channel channel when channel.IsChannel => "频道",
        TL.Channel => "超级群",
        _ => "未知"
    };
}
