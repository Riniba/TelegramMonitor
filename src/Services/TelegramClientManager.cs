namespace TelegramMonitor;

public sealed class TelegramClientManager : ISingleton, IAsyncDisposable
{
    private readonly ILogger<TelegramClientManager> _logger;
    private Client _client;
    private UpdateManager _manager;
    private readonly SystemCacheServices _systemCacheServices;

    private string _phone;
    private string _proxyUrl;
    private ProxyType _proxyType = ProxyType.None;
    private Client.TcpFactory _directTcp;
    private long _sendChatId;
    private volatile bool _running;

    public readonly Dictionary<long, User> _users = new Dictionary<long, User>();
    public readonly Dictionary<long, ChatBase> _chats = new Dictionary<long, ChatBase>();
    public bool IsMonitoring => _running && IsLoggedIn;
    public bool IsLoggedIn => _client is { Disconnected: false } && _client.User != null;
    public string GetPhone => _phone ?? string.Empty;

    public void SetSendChatId(long chatId) => _sendChatId = chatId;

    public TelegramClientManager(ILogger<TelegramClientManager> logger, SystemCacheServices systemCacheServices)
    {
        _logger = logger;
        _systemCacheServices = systemCacheServices;
    }

    private string User(long id) => _users.TryGetValue(id, out var user) ? user.ToString() : $"User {id}";

    private string Chat(long id) => _chats.TryGetValue(id, out var chat) ? chat.ToString() : $"Chat {id}";

    private string Peer(Peer peer) => UserOrChat(peer)?.ToString() ?? $"Peer {peer?.ID}";

    public async Task<LoginState> LoginAsync(string phoneNumber, string loginInfo)
    {
        phoneNumber = (phoneNumber ?? string.Empty).Replace(" ", "").Trim();
        loginInfo = (loginInfo ?? string.Empty).Replace(" ", "").Trim();
        if (!phoneNumber.IsE164Phone())
            throw new ArgumentException("手机号码格式不正确", nameof(phoneNumber));

        if (phoneNumber != _phone && _client != null)
        {
            await _client.DisposeAsync();
            _client = null;
            _manager = null;
        }

        _phone = phoneNumber;

        EnsureClientCreated();
        ApplyProxy();
        var firstArg = string.IsNullOrWhiteSpace(loginInfo) ? phoneNumber : loginInfo;
        var result = await _client.Login(firstArg);

        while (result is "name")
            result = await _client.Login("TelegramMonitor");

        return result switch
        {
            "verification_code" => LoginState.WaitingForVerificationCode,
            "password" => LoginState.WaitingForPassword,
            null => IsLoggedIn ? LoginState.LoggedIn : LoginState.NotLoggedIn,
            _ => LoginState.NotLoggedIn
        };
    }

    public async Task<LoginState> ConnectAsync(string phoneNumber)
    {
        await _client.LoginUserIfNeeded();
        if (IsLoggedIn)
        {
            return LoginState.LoggedIn;
        }
        else
        {
            return await LoginAsync(phoneNumber, string.Empty);
        }
    }

    public async Task<LoginState> SetProxyAsync(ProxyType type, string url)
    {
        _proxyType = type;
        _proxyUrl = url;

        if (_client == null) return LoginState.NotLoggedIn;

        if (!IsLoggedIn)
        {
            ApplyProxy();
            return LoginState.NotLoggedIn;
        }

        string phone = _phone;

        await _client.DisposeAsync();
        _client = null;
        _manager = null;

        EnsureClientCreated();
        ApplyProxy();
        var result = await _client.Login(phone);

        while (result is "name")
            result = await _client.Login("TelegramMonitor");

        return result switch
        {
            "verification_code" => LoginState.WaitingForVerificationCode,
            "password" => LoginState.WaitingForPassword,
            null => IsLoggedIn ? LoginState.LoggedIn : LoginState.NotLoggedIn,
            _ => LoginState.NotLoggedIn
        };
    }

    public async Task<Client> GetClientAsync()
    {
        if (_client.Disconnected) await _client.Login(_phone);
        if (!IsLoggedIn) throw new InvalidOperationException("未登录");

        return _client;
    }

    public async Task<List<DisplayDialogs>> DialogsAsync()
    {
        if (_client == null)
            throw new InvalidOperationException("未登录");

        var dialogs = await _client.Messages_GetAllDialogs();

        var availableChats = dialogs.chats.Values
            .Where(c => c.IsActive && CanSendMessagesFast(c))
            .ToList();

        return availableChats.Select(c => new DisplayDialogs
        {
            Id = c.ID,
            DisplayTitle = $"[{GetChatType(c)}]{(string.IsNullOrEmpty(c.MainUsername) ? "" : $"(@{c.MainUsername})")}{c.Title}",
        }).ToList();
    }

    public async Task<MonitorStartResult> StartTaskAsync()
    {
        if (_sendChatId == 0) return MonitorStartResult.MissingTarget;
        if (!IsLoggedIn) return MonitorStartResult.Error;
        if (IsMonitoring) return MonitorStartResult.AlreadyRunning;

        try
        {
            var manager = GetUpdateManagerAsync(HandleUpdateAsync);
            var dialogs = await _client.Messages_GetAllDialogs();
            dialogs.CollectUsersChats(_users, _chats);
            if (_client.User == null) return MonitorStartResult.NoUserInfo;

            _running = true;
            _logger.LogInformation("监控启动成功");
            return MonitorStartResult.Started;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "监控启动失败");
            _running = false;
            return MonitorStartResult.Error;
        }
    }

    public async Task StopTaskAsync()
    {
        _running = false;
        if (_manager != null)
        {
            await _client.DisposeAsync();
            _manager = null;
            _client = null;
            await LoginAsync(_phone, string.Empty);
        }
        _logger.LogError("主动停止监控");
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null) await _client.DisposeAsync();
        _client = null;
        _manager = null;
    }

    private UpdateManager GetUpdateManagerAsync(Func<Update, Task> onUpdate)
    {
        if (_manager != null) return _manager;
        _manager = _client.WithUpdateManager(onUpdate, collector: new MyCollector(_users, _chats));
        return _manager;
    }

    private void EnsureClientCreated()
    {
        if (_client != null) return;
        _client = new Client(
            TelegramMonitorConstants.ApiId,
            TelegramMonitorConstants.ApiHash,
            Path.Combine(TelegramMonitorConstants.SessionPath, $"{_phone}.session"));
        _directTcp = _client.TcpHandler;
    }

    private void ApplyProxy()
    {
        if (_client == null) return;

        switch (_proxyType)
        {
            case ProxyType.Socks5:
                _client.TcpHandler = (host, port) =>
                {
                    var p = Socks5ProxyClient.Parse(_proxyUrl);
                    return Task.FromResult(p.CreateConnection(host, port));
                };
                _client.MTProxyUrl = null;
                break;

            case ProxyType.MTProxy:
                _client.MTProxyUrl = _proxyUrl;
                _client.TcpHandler = _directTcp;
                break;

            case ProxyType.None:
            default:
                _client.TcpHandler = _directTcp;
                _client.MTProxyUrl = null;
                break;
        }
    }

    private IPeerInfo UserOrChat(Peer peer)
    {
        if (peer is PeerUser pu)
            return _users.TryGetValue(pu.user_id, out var u) ? u : null;
        if (peer is PeerChat pc)
            return _chats.TryGetValue(pc.chat_id, out var c) ? c : null;
        if (peer is PeerChannel pch)
            return _chats.TryGetValue(pch.channel_id, out var c2) ? c2 : null;
        return null;
    }

    private async Task EnsureUsersAndChatsFromMessageAsync(Message message)
    {
        if (message.From is PeerUser peerUser)
        {
            try
            {
                var user = _users.GetValueOrDefault(message.from_id);
                if (user.flags.HasFlag(TL.User.Flags.min))
                {
                    var full = await _client.Users_GetFullUser(new InputUserFromMessage()
                    {
                        user_id = message.From.ID,
                        msg_id = message.ID,
                        peer = _chats[message.Peer.ID].ToInputPeer()
                    });
                    full.CollectUsersChats(_users, _chats);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("拉取用户 {UserId} 失败: {@Exception}", peerUser.user_id, ex);
            }
        }
        if (message.Peer is PeerUser peerPeerUser)
        {
            try
            {
                var user = _users.GetValueOrDefault(message.peer_id);
                if (user.flags.HasFlag(TL.User.Flags.min))
                {
                    var full = await _client.Users_GetFullUser(new InputUserFromMessage()
                    {
                        user_id = message.From.ID,
                        msg_id = message.ID,
                        peer = _users[message.Peer.ID].ToInputPeer()
                    });
                    full.CollectUsersChats(_users, _chats);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("拉取用户 {UserId} 失败: {@Exception}", peerPeerUser.user_id, ex);
            }
        }
        if (message.Peer is PeerChannel peerChannel)
        {
            try
            {
                var channel = _chats.GetValueOrDefault(message.peer_id) as TL.Channel;
                if (channel.flags.HasFlag(TL.Channel.Flags.min))
                {
                    var full = await _client.Channels_GetFullChannel(new InputChannelFromMessage()
                    {
                        channel_id = peerChannel.channel_id,
                        msg_id = message.ID,
                        peer = _chats[message.Peer.ID].ToInputPeer()
                    });
                    full.CollectUsersChats(_users, _chats);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("拉取频道 {channel_id} 失败: {@Exception}", peerChannel.channel_id, ex);
            }
        }
    }

    private async Task HandleUpdateAsync(Update update)
    {
        try
        {
            switch (update)
            {
                case UpdateNewMessage unm:
                    await HandleMessageAsync(unm.message);
                    break;

                case UpdateEditMessage uem:
                    _logger.LogInformation(
                        "{User} edited a message in {Chat}",
                        User(uem.message.From),
                        Chat(uem.message.Peer));
                    break;

                case UpdateDeleteChannelMessages udcm:
                    _logger.LogInformation("{Count} message(s) deleted in {Chat}",
                                           udcm.messages.Length,
                                           Chat(udcm.channel_id));
                    break;

                case UpdateDeleteMessages udm:
                    _logger.LogInformation("{Count} message(s) deleted",
                                           udm.messages.Length);
                    break;

                case UpdateUserTyping uut:
                    _logger.LogInformation("{User} is {Action}",
                                           User(uut.user_id), uut.action);
                    break;

                case UpdateChatUserTyping ucut:
                    _logger.LogInformation("{Peer} is {Action} in {Chat}",
                                           Peer(ucut.from_id), ucut.action,
                                           Chat(ucut.chat_id));
                    break;

                case UpdateChannelUserTyping ucut2:
                    _logger.LogInformation("{Peer} is {Action} in {Chat}",
                                           Peer(ucut2.from_id), ucut2.action,
                                           Chat(ucut2.channel_id));
                    break;

                case UpdateChatParticipants { participants: ChatParticipants cp }:
                    _logger.LogInformation("{Count} participants in {Chat}",
                                           cp.participants.Length,
                                           Chat(cp.chat_id));
                    break;

                case UpdateUserStatus uus:
                    _logger.LogInformation("{User} is now {Status}",
                                           User(uus.user_id),
                                           uus.status.GetType().Name[10..]);
                    break;

                case UpdateUserName uun:
                    _logger.LogInformation("{User} changed profile name: {FN} {LN}",
                                           User(uun.user_id),
                                           uun.first_name, uun.last_name);
                    break;

                case UpdateUser uu:
                    _logger.LogInformation("{User} changed infos/photo",
                                           User(uu.user_id));
                    break;

                default:
                    _logger.LogInformation(update.GetType().Name);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 Update 时发生异常");
        }
    }

    private string GetChatType(ChatBase chat) => chat switch
    {
        TL.Chat => "Chat",
        TL.Channel ch when ch.IsChannel => "Channel",
        TL.Channel => "Group",
        _ => "Unknown"
    };

    private bool CanSendMessages(ChatBase chat) => chat switch
    {
        TL.Chat small => !small.IsBanned(ChatBannedRights.Flags.send_messages),
        TL.Channel ch when ch.IsChannel => !ch.IsBanned(ChatBannedRights.Flags.send_messages),
        TL.Channel group => !group.IsBanned(ChatBannedRights.Flags.send_messages),
        _ => false
    };

    private static bool CanSendMessagesFast(ChatBase chat)
    {
        switch (chat)
        {
            case Chat small:
                return !small.IsBanned(ChatBannedRights.Flags.send_messages);

            case Channel ch when ch.IsChannel:
                if (ch.flags.HasFlag(Channel.Flags.left)) return false;
                if (ch.flags.HasFlag(Channel.Flags.creator)) return true;
                return ch.admin_rights?.flags.HasFlag(ChatAdminRights.Flags.post_messages) == true;

            case Channel ch:
                if (ch.flags.HasFlag(Channel.Flags.left)) return false;
                if (ch.flags.HasFlag(Channel.Flags.creator)) return true;
                if (ch.admin_rights?.flags != 0) return true;
                return !ch.IsBanned(ChatBannedRights.Flags.send_messages);

            default:
                return false;
        }
    }

    private async Task HandleMessageAsync(MessageBase messageBase)
    {
        try
        {
            switch (messageBase)
            {
                case Message m:
                    await HandleTelegramMessageAsync(m);
                    break;

                case MessageService ms:
                    _logger.LogInformation("{From} in {Peer} [{Action}]",
                                         UserOrChat(ms.from_id),
                                         UserOrChat(ms.peer_id),
                                         ms.action.GetType().Name[13..]);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理消息时发生异常");
        }
    }

    private async Task HandleTelegramMessageAsync(TL.Message message)
    {
        if (string.IsNullOrWhiteSpace(message.message))
        {
            return;
        }
        var keywords = await _systemCacheServices.GetKeywordsAsync() ?? new List<KeywordConfig>();
        var matchedKeywords = KeywordMatchExtensions.MatchText(message.message, keywords);

        if (matchedKeywords.Any(k => k.KeywordAction == KeywordAction.Exclude))
        {
            _logger.LogInformation("消息 {message}包含排除关键词 {keywords}，跳过",message.message, string.Join(",", matchedKeywords));
            return;
        }

        matchedKeywords = matchedKeywords
            .Where(k => k.KeywordAction == KeywordAction.Monitor)
            .ToList();

        if (matchedKeywords.Count == 0)
        {
            _logger.LogInformation("消息 {message} 无匹配关键词，跳过", message.message);
            return;
        }

    if (message.Peer is null)
        {
            _logger.LogWarning("消息 {MessageId} 没有 Peer 信息，无法处理", message.ID);
            return;
        }

        await EnsureUsersAndChatsFromMessageAsync(message);

        var sendEntity = BuildSendEntity(message);
        if (sendEntity is null)
        {
            _logger.LogWarning("无法解析消息的来源或发送者，跳过");
            return;
        }

        _logger.LogInformation(
            "{Nick} (ID:{Uid}) 在 {Chat} (ID:{Chatid}) 中发送：{Text}",
            sendEntity.SendTitle, sendEntity.SendId,
            sendEntity.FromTitle, sendEntity.FromId, message.message);

        var matchedUserKeywords = KeywordMatchExtensions.MatchUser(
            sendEntity.SendId,
            sendEntity.SendUserNames?.ToList() ?? new List<string>(),
            keywords);

        if (matchedUserKeywords.Any(k => k.KeywordAction == KeywordAction.Exclude))
        {
            _logger.LogInformation(" (ID:{Uid}) 在排除列表内，跳过", sendEntity.SendId);
            return;
        }

        var finalKeywords = matchedUserKeywords.Any(k => k.KeywordAction == KeywordAction.Monitor)
            ? (IReadOnlyList<KeywordConfig>)matchedUserKeywords
            : matchedKeywords;

        var msgContent = message.FormatForMonitor(
            sendEntity,
            finalKeywords);

        await SendMonitorMessageAsync(message, msgContent);
    }

    private SendMessageEntity? BuildSendEntity(TL.Message message)
    {
        if (message.Peer is null) return null;

        if (!TryResolvePeer(message.Peer, out var fromId, out var fromTitle, out var fromMain, out var fromUserNames))
        {
            return null;
        }

        long sendId; string sendTitle; string sendMain; IEnumerable<string> sendUserNames;
        if (message.From is null)
        {
            var isChannelPostFlag = message.flags.HasFlag(TL.Message.Flags.post);
            var isBroadcastChannel =
                _chats.TryGetValue(fromId, out var fromChat)
                && fromChat is TL.Channel fromChannel
                && fromChannel.IsChannel;

            if (isChannelPostFlag || isBroadcastChannel)
            {
                sendId = fromId;
                sendTitle = fromTitle;
                sendMain = fromMain;
                sendUserNames = fromUserNames;
            }
            else if (message.Peer is TL.PeerUser && message.flags.HasFlag(TL.Message.Flags.out_))
            {
                var me = _client.User;
                if (me is null) return null;

                sendId = me.ID;
                sendTitle = $"{me.first_name} {me.last_name}".Trim();
                sendMain = me.MainUsername;
                sendUserNames = me.ActiveUsernames ?? Enumerable.Empty<string>();
            }
            else
            {
                sendId = fromId;
                sendTitle = fromTitle;
                sendMain = fromMain;
                sendUserNames = fromUserNames;
            }
        }
        else if (!TryResolvePeer(message.From, out sendId, out sendTitle, out sendMain, out sendUserNames))
        {
            return null;
        }

        return new SendMessageEntity
        {
            FromId = fromId,
            FromTitle = fromTitle,
            FromMainUserName = fromMain,
            FromUserNames = fromUserNames,

            SendId = sendId,
            SendTitle = sendTitle,
            SendUserNames = sendUserNames
        };
    }

    private async Task SendMonitorMessageAsync(Message originalMessage, string content)
    {
        try
        {
            long sendChatId = _sendChatId;
            if (sendChatId == 0)
            {
                _logger.LogWarning("未设置发送目标");
                return;
            }

            var chat = _chats.GetValueOrDefault(sendChatId);
            if (chat == null)
            {
                _logger.LogWarning("无法找到 ID 为 {Id} 的发送目标", sendChatId);
                return;
            }
            var entities = _client.MarkdownToEntities(ref content, users: _users);
            await _client.SendMessageAsync(
                chat, content,
                preview: Client.LinkPreview.Disabled,
                entities: entities,
                media: originalMessage.media?.ToInputMedia());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送监控消息失败");
        }
    }

    private bool TryResolvePeer(
        TL.Peer peer,
        out long id, out string title,
        out string mainUserName, out IEnumerable<string> allUserNames)
    {
        id = 0; title = null; mainUserName = null; allUserNames = [];
        if (peer is null) return false;

        if (peer is TL.PeerUser pu)
        {
            if (_users.TryGetValue(pu.user_id, out var u))
            {
                id = u.ID;
                title = u.DisplayName();
                mainUserName = u.MainUsername;
                allUserNames = u.ActiveUsernames ?? Enumerable.Empty<string>(); ;
                return true;
            }
            return false;
        }
        if (peer is TL.PeerChat pc)
        {
            if (_chats.TryGetValue(pc.chat_id, out var smallGroup))
            {
                id = smallGroup.ID;
                title = smallGroup.Title;
                mainUserName = smallGroup.MainUsername;
                allUserNames = smallGroup.MainUsername != null
                ? new[] { smallGroup.MainUsername }
                : Enumerable.Empty<string>();
                ;
                return true;
            }
            return false;
        }
        if (peer is TL.PeerChannel pch)
        {
            if (_chats.TryGetValue(pch.channel_id, out var chatBase))
            {
                id = chatBase.ID;
                title = chatBase.Title;
                mainUserName = chatBase.MainUsername;
                allUserNames = chatBase is TL.Channel ch
                ? (ch.ActiveUsernames ?? Enumerable.Empty<string>())
                : (chatBase.MainUsername != null
                    ? new[] { chatBase.MainUsername }
                    : Enumerable.Empty<string>());
                return true;
            }
            return false;
        }
        return false;
    }
}