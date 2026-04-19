namespace TelegramMonitor;

public interface IAdminAuthService
{
    Task<AdminLoginResult> LoginAsync(AdminLoginRequest request);
    AdminProfileResult GetCurrentUser(ClaimsPrincipal user);
}

