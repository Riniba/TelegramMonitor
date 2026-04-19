using Furion.ConfigurableOptions;

namespace TelegramMonitor;

[OptionsSettings("Telegram")]
public class TelegramOptions : IConfigurableOptions
{
    public int DefaultApiId { get; set; }
    public string DefaultApiHash { get; set; } = string.Empty;
    public string SessionsPath { get; set; } = "session";
}
