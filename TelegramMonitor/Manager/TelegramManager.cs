﻿using Spectre.Console;
using System;
using System.Data;
using TL;
using WTelegram;

namespace TelegramMonitor;

// Telegram客户端交互类
public class TelegramManager
{
    private readonly Client _client;
    private readonly PeriodicTaskManager _taskManager;
    private long _sendChatId;
    private UpdateManager? _manager;
    private User? _myUser;

    public TelegramManager(Client client)
    {
        _client = client;
        _taskManager = new PeriodicTaskManager();
    }

    private ChatBase? ChatBase(long id) => _manager?.Chats.GetValueOrDefault(id);

    private User? User(long id) => _manager?.Users.GetValueOrDefault(id);

    private IPeerInfo? Peer(Peer peer) => _manager?.UserOrChat(peer);

    // 处理Telegram的Update事件
    private async Task Client_OnUpdate(Update update)
    {
        try
        {
            await ProcessUpdateAsync(update);
        }
        catch (Exception ex)
        {
            LogExtensions.Error($"处理Update时发生异常: {ex.Message}");
        }
    }

    // 处理更新事件
    private async Task ProcessUpdateAsync(Update update)
    {
        switch (update)
        {
            case UpdateNewMessage unm:
                await HandleMessageAsync(unm.message);
                break;

            case UpdateEditMessage uem:
                LogExtensions.Debug($"{User(uem.message.From)} edited a message  in {ChatBase(uem.message.Peer)}");
                break;

            case UpdateDeleteChannelMessages udcm:
                LogExtensions.Debug($"{udcm.messages.Length} messages deleted in {ChatBase(udcm.channel_id)}");
                break;

            case UpdateDeleteMessages udm:
                LogExtensions.Debug($"{udm.messages.Length} messages deleted ");
                break;

            case UpdateUserTyping uut:
                LogExtensions.Debug($"{User(uut.user_id)} is {uut.action}");
                break;

            case UpdateChatUserTyping ucut:
                LogExtensions.Debug($"{Peer(ucut.from_id)} is {ucut.action} in {ChatBase(ucut.chat_id)}");
                break;

            case UpdateChannelUserTyping ucut2:
                LogExtensions.Debug($"{Peer(ucut2.from_id)} is {ucut2.action} in {ChatBase(ucut2.channel_id)}");
                break;

            case UpdateChatParticipants { participants: ChatParticipants cp }:
                LogExtensions.Debug($"{cp.participants.Length} participants in {ChatBase(cp.chat_id)}");
                break;

            case UpdateUserStatus uus:
                LogExtensions.Debug($"{User(uus.user_id)} is now {uus.status.GetType().Name[10..]}");
                break;

            case UpdateUserName uun:
                LogExtensions.Debug($"{User(uun.user_id)} changed profile name: {uun.first_name} {uun.last_name}");
                break;

            case UpdateUser uu:
                LogExtensions.Debug($"{User(uu.user_id)} changed infos/photo");
                break;

            default:
                LogExtensions.Debug(update.GetType().Name);
                break;
        }
    }

    // 处理接收到的消息
    private async Task HandleMessageAsync(MessageBase messageBase, bool edit = false)
    {
        if (edit)
        {
            return;
        }
        try
        {
            switch (messageBase)
            {
                case Message m:
                    await HandleTLMessageAsync(m);
                    break;

                case MessageService ms:
                    LogExtensions.Debug($"{Peer(ms.from_id)} in {Peer(ms.peer_id)} [{ms.action.GetType().Name[13..]}]");
                    break;
            }
        }
        catch (Exception ex)
        {
            LogExtensions.Error($"处理消息时发生异常: {ex.Message}");
        }
    }

    // 处理普通消息
    private async Task HandleTLMessageAsync(Message m)
    {
        // 尝试获取有效的用户和聊天对象
        if (!TryGetValidGroupChatAndUser(m, out var groupChat, out var user))
            return;
        await HandleKeywordMatchesAsync(groupChat!, user!, m);
    }

    // 执行登录流程
    public async Task DoLoginAsync(string loginInfo)
    {
        while (_client.User == null)
        {
            var what = await _client.Login(loginInfo);
            switch (what)
            {
                case "verification_code":
                    LogExtensions.Prompts("验证码: ");
                    loginInfo = Console.ReadLine() ?? string.Empty;
                    break;

                case "name":
                    loginInfo = "by riniba";
                    break;

                case "password":
                    LogExtensions.Prompts("二级密码: ");
                    loginInfo = Console.ReadLine() ?? string.Empty;
                    break;

                default:
                    loginInfo = string.Empty;
                    break;
            }
        }

        _myUser = _client.User;
        LogExtensions.Info($"监控人员: {_myUser} (id {_myUser.id}) 启动成功!");

        // 获取所有对话信息填充 Manager 的字典
        var dialogs = await _client.Messages_GetAllDialogs();

        // 载入关键词列表和黑名单
        FileExtensions.LoadKeywords(Constants.KEYWORDS_FILE_PATH);
        FileExtensions.LoadBlacklistKeywords(Constants.BLACKLIST_KEYWORDS_FILE_PATH);
        FileExtensions.LoadBlacklistUsers(Constants.BLACKLIST_USERS_FILE_PATH);

        // 从 API 获取外部数据（如广告内容）
        await HttpExtensions.FetchAndProcessDataAsync();

        // 选择可发送消息的频道或群组列表
        await GetManagedChatAsync(dialogs);
    }

    //开始工作
    private void StartTelegram(Messages_Dialogs dialogs)
    {
        LogExtensions.Info("开始工作!...");
        _manager = _client.WithUpdateManager(Client_OnUpdate);
        dialogs.CollectUsersChats(_manager.Users, _manager.Chats);
        // 启动定时任务
        _taskManager.Start();
        Console.ReadKey();
    }

    // 获取可发送消息的频道或群组列表
    private async Task GetManagedChatAsync(Messages_Dialogs dialogs)
    {
        var availableChats = new List<ChatBase>();
        ChatBase? selectedChat = null;

        // 创建选择提示
        var prompt = new SelectionPrompt<ChatBase>()
            .Title("选择监控消息发布的目标")
            .PageSize(10);

        // 添加符合条件的聊天到列表
        foreach (var (id, chat) in dialogs.chats)
        {
            if (!chat.IsActive) continue;

            bool canSendMessages = false;

            switch (chat)
            {
                case Chat smallgroup:
                    canSendMessages = !smallgroup.IsBanned(ChatBannedRights.Flags.send_messages);
                    break;

                case Channel channel when channel.IsChannel:
                    canSendMessages = !channel.IsBanned(ChatBannedRights.Flags.send_messages);
                    break;

                case Channel group:
                    canSendMessages = !group.IsBanned(ChatBannedRights.Flags.send_messages);
                    break;

                default:
                    canSendMessages = false;
                    break;
            }

            if (canSendMessages)
            {
                availableChats.Add(chat);
                prompt.AddChoice(chat);
            }
        }
        if (availableChats.Count == 0)
        {
            LogExtensions.Error("未找到任何可发送消息的频道或群组！");
            return;
        }

    select:
        // 选择聊天
        while (selectedChat == null)
        {
            LogExtensions.Prompts("选择要发送监控消息的目标:");

            selectedChat = AnsiConsole.Prompt(prompt);

            if (selectedChat == null)
            {
                LogExtensions.Error("无效的选择，请重新选择一个有效的目标。");
            }
        }

        LogExtensions.Info($"您已选择：{selectedChat.Title} (ID: {selectedChat.ID})");
        try
        {
            await _client.SendMessageAsync(selectedChat, "软件就绪!开始监控！");
            _sendChatId = selectedChat.ID;
            StartTelegram(dialogs);
        }
        catch (Exception e)
        {
            LogExtensions.Error($"{e.Message}");
            selectedChat = null;
            goto select;
        }
    }

    //处理消息关键词
    private async Task HandleKeywordMatchesAsync(ChatBase chat, User user, Message message)
    {
        if (string.IsNullOrWhiteSpace(message.message))
        {
            return;
        }

        // 检查用户是否在黑名单中
        if (IsBlacklistedUser(user))
        {
            LogExtensions.Debug($"用户 {GetTelegramNickName(user)} 在黑名单中，跳过处理");
            return;
        }

        // 检查消息是否包含黑名单关键词
        if (FileExtensions.ContainsBlacklistKeyword(message.message))
        {
            LogExtensions.Debug($"消息包含黑名单关键词，跳过处理");
            return;
        }

        LogExtensions.Debug($"{GetTelegramNickName(user)} （ID:{user.id}） 在 {chat.Title} 中发送：{message.message}");
        // 使用 GetMatchingKeywords 方法处理消息，返回匹配的关键词
        var matchedKeywords = FileExtensions.GetMatchingKeywords(message.message.ToLower(), Constants.KEYWORDS);
        if (matchedKeywords.Count == 0)
        {
            LogExtensions.Debug($"无匹配关键词，跳过");
            return;  // 如果没有匹配的关键词，直接返回
        }

        // 构建消息内容并发送
        var messageContent = BuildMessageContent(chat, user, message, matchedKeywords);
        await SendMonitorMessageAsync(messageContent, message);
    }

    //构建发送的消息内容
    private string BuildMessageContent(ChatBase chat, User user, Message message, List<string> keywords)
    {
        var text = _client.EntitiesToHtml(message.message, message.entities);
        var formattedData = string.Join("\n", Constants.DATA.Select(line => $"<b>{line}</b>"));
        var keywordDisplay = string.Join(", ", keywords.Select(k => $"#{k.Replace("?", "")}"));
        LogExtensions.Warning($"匹配到关键词{keywordDisplay}");

        return $@"
<b>命中关键词：</b>{keywordDisplay}
用户ID：<code>{user.id}</code>
用户：{GetTelegramUserLink(user)}  {GetTelegramUserName(user)}
来源：<code>【{chat.Title}】</code>  {chat.MainUsername?.Insert(0, "@") ?? "无"}
时间：<code>{message.Date.AddHours(8):yyyy-MM-dd HH:mm:ss}</code>
内容：<b>{text}</b>
链接：<a href=""https://t.me/{chat.MainUsername ?? $"c/{chat.ID}"}/{message.id}"">【直达】</a>
--------------------------------
{formattedData}";
    }

    //发送信息到指定会话
    private async Task SendMonitorMessageAsync(string content, Message originalMessage)
    {
        try
        {
            var chat = ChatBase(_sendChatId);
            if (chat == null) return;

            var entities = _client.HtmlToEntities(ref content, users: _manager?.Users);
            await _client.SendMessageAsync(chat, content,
                preview: Client.LinkPreview.Disabled,
                entities: entities,
                media: originalMessage.media?.ToInputMedia());
        }
        catch (Exception ex)
        {
            LogExtensions.Error($"发送消息失败: {ex.Message}");
        }
    }

    // 获取用户名
    private string GetTelegramUserName(User user) =>
        string.Join(" ", user.ActiveUsernames?.Select(u => $"@{u}") ?? Array.Empty<string>());

    // 获取用户昵称
    private string GetTelegramNickName(User user) =>
        (user.first_name + user.last_name)?.Replace("<", "").Replace(">", "") ?? string.Empty;

    // 获取用户链接
    private string GetTelegramUserLink(User user)
    {
        var nickName = GetTelegramNickName(user);
        var displayName = string.IsNullOrEmpty(nickName) ? user.id.ToString() : nickName;
        return $"<a href=\"tg://user?id={user.id}\">{displayName}</a>";
    }

    // 验证消息来源的有效性
    private bool TryGetValidGroupChatAndUser(
        Message message,
        out ChatBase? groupChat,
        out User? user)
    {
        groupChat = null;
        user = null;

        if (_manager == null || message.from_id == null) return false;

        user = User(message.from_id);
        if (user == null || user.IsBot) return false;

        var chatBase = ChatBase(message.peer_id);
        if (chatBase == null || !chatBase.IsGroup || string.IsNullOrWhiteSpace(message.message))
            return false;

        groupChat = chatBase;
        return true;
    }

    //验证用户是否在黑名单中
    private bool IsBlacklistedUser(User user)
    {
        return Constants.BLACKLIST_USERS.Any(blacklisted =>
            blacklisted == user.id.ToString() ||
            user.ActiveUsernames?.Any(username => username.Equals(blacklisted, StringComparison.OrdinalIgnoreCase)) == true);
    }
}