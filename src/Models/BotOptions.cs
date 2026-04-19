using Furion.ConfigurableOptions;

namespace TelegramMonitor;

[OptionsSettings("Bot")]
public class BotOptions : IConfigurableOptions
{
    public bool Enabled { get; set; }

    public List<string> Tokens { get; set; } = [];
}
