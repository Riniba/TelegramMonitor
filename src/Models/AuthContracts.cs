namespace TelegramMonitor;

public record AdminLoginRequest(string Username, string Password);

public record AdminLoginResult(
    string Username,
    string Role,
    DateTime ExpiresAt);

public record AdminProfileResult(
    string Username,
    string Role);

public record AdminLogoutResult(
    bool Success,
    string Message);
