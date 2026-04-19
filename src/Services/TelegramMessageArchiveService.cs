namespace TelegramMonitor;

public class TelegramMessageArchiveService : ITelegramMessageArchiveService
{
    private readonly ISqlSugarClient _db;

    public TelegramMessageArchiveService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<TelegramMessageRecord> ArchiveAsync(
        int accountId,
        UpdateManager manager,
        string updateType,
        MessageBase messageBase,
        bool isEdited)
    {
        var record = BuildRecord(accountId, manager, updateType, messageBase, isEdited);
        await _db.Insertable(record).ExecuteCommandAsync();
        return record;
    }

    public Task<TelegramMessageRecord?> FindByIdAsync(int id) =>
        _db.Queryable<TelegramMessageRecord>()
            .FirstAsync(x => x.Id == id);

    public async Task<PagedResult<TelegramMessageRecord>> QueryPageAsync(TelegramMessageQueryRequest request)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 50 : Math.Min(request.PageSize, 200);

        var query = _db.Queryable<TelegramMessageRecord>();

        if (request.AccountId.HasValue)
            query = query.Where(x => x.AccountId == request.AccountId.Value);

        if (request.ChatId.HasValue)
            query = query.Where(x => x.ChatId == request.ChatId.Value);

        if (request.SenderId.HasValue)
            query = query.Where(x => x.SenderId == request.SenderId.Value);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(x =>
                x.Text.Contains(keyword) ||
                x.ChatTitle.Contains(keyword) ||
                x.SenderTitle.Contains(keyword));
        }

        if (request.IsOutgoing.HasValue)
            query = query.Where(x => x.IsOutgoing == request.IsOutgoing.Value);

        if (request.IsEdited.HasValue)
            query = query.Where(x => x.IsEdited == request.IsEdited.Value);

        if (request.IsChannelPost.HasValue)
            query = query.Where(x => x.IsChannelPost == request.IsChannelPost.Value);

        RefAsync<int> total = 0;
        var items = await query
            .OrderByDescending(x => x.Id)
            .ToPageListAsync(page, pageSize, total);

        return new PagedResult<TelegramMessageRecord>(total, page, pageSize, items);
    }

    private static TelegramMessageRecord BuildRecord(
        int accountId,
        UpdateManager manager,
        string updateType,
        MessageBase messageBase,
        bool isEdited)
    {
        var peer = GetPeer(messageBase);
        var fromPeer = GetFromPeer(messageBase);
        var chat = ResolvePeer(manager, peer);
        var sender = ResolvePeer(manager, fromPeer);
        var reply = GetReplyHeader(messageBase);
        var text = GetText(messageBase);
        var serviceAction = GetServiceActionType(messageBase);
        var mediaType = GetMediaType(messageBase);
        var messageDate = GetMessageDate(messageBase);

        var payload = new
        {
            accountId,
            updateType,
            messageType = messageBase.GetType().Name,
            telegramMessageId = messageBase.ID,
            chat,
            sender,
            text,
            serviceAction,
            mediaType,
            replyToMessageId = reply?.reply_to_msg_id,
            threadId = reply?.reply_to_top_id,
            date = messageDate
        };

        return new TelegramMessageRecord
        {
            AccountId = accountId,
            UpdateType = updateType,
            MessageType = messageBase.GetType().Name,
            TelegramMessageId = messageBase.ID,
            ChatId = chat.Id,
            ChatType = chat.PeerType,
            ChatTitle = chat.Title,
            ChatUsername = chat.MainUsername,
            SenderId = sender.Id,
            SenderTitle = sender.Title,
            SenderUsername = sender.MainUsername,
            IsOutgoing = IsOutgoing(messageBase),
            IsEdited = isEdited,
            IsChannelPost = IsChannelPost(messageBase),
            MessageDate = messageDate,
            ReceivedAt = ChinaTime.Now,
            Text = text,
            MediaType = mediaType,
            ReplyToMessageId = reply?.reply_to_msg_id,
            ThreadId = reply?.reply_to_top_id,
            ServiceActionType = serviceAction,
            RawJson = JsonSerializer.Serialize(payload)
        };
    }

    private static DateTime? GetMessageDate(MessageBase messageBase)
    {
        var date = messageBase.Date;
        if (date == default)
            return null;

        return ChinaTime.ToChinaTime(DateTime.SpecifyKind(date, DateTimeKind.Utc));
    }

    private static string? GetText(MessageBase messageBase) =>
        messageBase switch
        {
            Message message => string.IsNullOrWhiteSpace(message.message) ? null : message.message.Trim(),
            MessageService service => service.action?.ToString(),
            _ => null
        };

    private static string? GetServiceActionType(MessageBase messageBase) =>
        messageBase is MessageService service ? service.action?.GetType().Name : null;

    private static string? GetMediaType(MessageBase messageBase) =>
        messageBase is Message message ? message.media?.GetType().Name : null;

    private static bool IsOutgoing(MessageBase messageBase) =>
        messageBase is Message message && message.flags.HasFlag(Message.Flags.out_);

    private static bool IsChannelPost(MessageBase messageBase) =>
        messageBase is Message message && message.flags.HasFlag(Message.Flags.post);

    private static MessageReplyHeader? GetReplyHeader(MessageBase messageBase) =>
        messageBase switch
        {
            Message message => message.reply_to as MessageReplyHeader,
            _ => null
        };

    private static Peer? GetPeer(MessageBase messageBase) =>
        messageBase switch
        {
            Message message => message.Peer,
            MessageService service => service.peer_id,
            _ => null
        };

    private static Peer? GetFromPeer(MessageBase messageBase) =>
        messageBase switch
        {
            Message message => message.From ?? message.Peer,
            MessageService service => service.from_id ?? service.peer_id,
            _ => null
        };

    private static TelegramPeerSnapshot ResolvePeer(UpdateManager manager, Peer? peer)
    {
        if (peer == null)
            return TelegramPeerSnapshot.Empty;

        switch (peer)
        {
            case PeerUser userPeer when manager.Users.TryGetValue(userPeer.user_id, out var user):
                return new TelegramPeerSnapshot(
                    user.ID,
                    "User",
                    user.DisplayName(),
                    user.MainUsername);

            case PeerChat chatPeer when manager.Chats.TryGetValue(chatPeer.chat_id, out var chat):
                return new TelegramPeerSnapshot(
                    chat.ID,
                    "Chat",
                    chat.Title,
                    chat.MainUsername);

            case PeerChannel channelPeer when manager.Chats.TryGetValue(channelPeer.channel_id, out var channel):
                return new TelegramPeerSnapshot(
                    channel.ID,
                    channel is Channel ch && ch.IsChannel ? "Channel" : "Group",
                    channel.Title,
                    channel.MainUsername);

            default:
                return new TelegramPeerSnapshot(peer.ID, peer.GetType().Name, null, null);
        }
    }

    private readonly record struct TelegramPeerSnapshot(
        long? Id,
        string? PeerType,
        string? Title,
        string? MainUsername)
    {
        public static TelegramPeerSnapshot Empty => new(null, null, null, null);
    }
}
