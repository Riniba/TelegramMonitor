using System.Threading.Channels;

namespace TelegramMonitor;

public record BotNotifyItem(
    long TargetChatId,
    string TargetChatTitle,
    string MessageText,
    string? CallbackData);

public class BotNotifyChannel
{
    private readonly System.Threading.Channels.Channel<BotNotifyItem> _channel =
        System.Threading.Channels.Channel.CreateUnbounded<BotNotifyItem>(
            new UnboundedChannelOptions { SingleReader = true });

    public ChannelWriter<BotNotifyItem> Writer => _channel.Writer;
    public ChannelReader<BotNotifyItem> Reader => _channel.Reader;
}
