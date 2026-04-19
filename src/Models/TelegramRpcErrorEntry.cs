namespace TelegramMonitor;

public class TelegramRpcErrorEntry
{
    public string OfficialKey { get; set; } = string.Empty;
    public string WTelegramKey { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string DescriptionEn { get; set; } = string.Empty;
    public string MessageZh { get; set; } = string.Empty;
}
