namespace TelegramMonitor;

public class AdminAuthService : IAdminAuthService
{
    public const string AdminRole = "Admin";
    private readonly IConfiguration _configuration;

    public AdminAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<AdminLoginResult> LoginAsync(AdminLoginRequest request)
    {
        if (request == null)
            throw Oops.Oh("登录请求不能为空");

        var username = request.Username?.Trim();
        var password = request.Password ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw Oops.Oh("用户名和密码不能为空");

        var options = GetOptions();
        if (!IsMatch(username, options.AdminUsername) || !IsMatch(password, options.AdminPassword))
            throw Oops.Oh("用户名或密码错误");

        var expiresAt = ChinaTime.ToChinaTime(DateTime.UtcNow.AddMinutes(GetCookieExpireMinutes(options)));
        return Task.FromResult(
            new AdminLoginResult(
                options.AdminUsername,
                AdminRole,
                expiresAt));
    }

    public AdminProfileResult GetCurrentUser(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var username = user.Claims
            .FirstOrDefault(claim => claim.Type == ClaimTypes.Name)
            ?.Value;

        if (string.IsNullOrWhiteSpace(username))
            throw Oops.Oh("当前用户未登录");

        return new AdminProfileResult(username, AdminRole);
    }

    private AuthOptions GetOptions() =>
        _configuration.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();

    public static int GetCookieExpireMinutes(AuthOptions options) =>
        options.CookieExpireMinutes <= 0 ? 480 : options.CookieExpireMinutes;

    private static bool IsMatch(string left, string right) =>
        string.Equals(left, right, StringComparison.Ordinal);
}
