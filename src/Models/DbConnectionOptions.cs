using Furion.ConfigurableOptions;

namespace TelegramMonitor;

[OptionsSettings("DbConnection")]
public class DbConnectionOptions : IConfigurableOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DbType { get; set; } = string.Empty;
}
