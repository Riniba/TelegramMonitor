namespace TelegramMonitor;

[ApiController]
[Route("api")]
[ApiDescriptionSettings(Tag = "auth", Description = "后台鉴权接口")]
public class AuthController : ControllerBase
{
    private readonly IAdminAuthService _adminAuthService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthController(
        IAdminAuthService adminAuthService,
        IHttpContextAccessor httpContextAccessor)
    {
        _adminAuthService = adminAuthService;
        _httpContextAccessor = httpContextAccessor;
    }

    [AllowAnonymous]
    [HttpPost("auth/login")]
    public async Task<AdminLoginResult> LoginAsync([FromBody] AdminLoginRequest request)
    {
        var result = await _adminAuthService.LoginAsync(request);
        var context = GetHttpContext();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, result.Username),
            new Claim(ClaimTypes.Name, result.Username),
            new Claim(ClaimTypes.Role, result.Role)
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow
            });

        return result;
    }

    [Authorize]
    [HttpGet("auth/me")]
    public AdminProfileResult Me()
    {
        var user = GetHttpContext().User;
        if (user.Identity?.IsAuthenticated != true)
            throw Oops.Oh("当前用户未登录");

        return _adminAuthService.GetCurrentUser(user);
    }

    [Authorize]
    [HttpPost("auth/logout")]
    public async Task<AdminLogoutResult> LogoutAsync()
    {
        await GetHttpContext().SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return new AdminLogoutResult(true, "退出成功");
    }

    private HttpContext GetHttpContext() =>
        _httpContextAccessor.HttpContext ?? throw Oops.Oh("当前请求上下文不可用");
}
