using Furion.ConfigurableOptions;

namespace TelegramMonitor;

[OptionsSettings("Auth")]
public class AuthOptions : IConfigurableOptions
{
    public string AdminUsername { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string CookieName { get; set; } = "telegrammonitor_admin";
    public int CookieExpireMinutes { get; set; } = 480;
    public bool SlidingExpiration { get; set; } = true;
}
