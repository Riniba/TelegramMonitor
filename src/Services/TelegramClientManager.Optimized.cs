// 优化后的日志记录方案示例

// 1. 添加日志配置类
public class LoggingOptions
{
    public bool LogMessageContent { get; set; } = false; // 默认不记录消息内容
    public bool LogUserIds { get; set; } = false; // 默认不记录用户ID
    public int MaxMessageLength { get; set; } = 50; // 最大消息长度
}

// 2. 优化消息处理日志
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
        // 优化：不记录完整消息内容
        _logger.LogInformation("消息包含排除关键词，跳过处理");
        return;
    }

    matchedKeywords = matchedKeywords
        .Where(k => k.KeywordAction == KeywordAction.Monitor)
        .ToList();

    if (matchedKeywords.Count == 0)
    {
        // 优化：简化日志
        _logger.LogDebug("消息无匹配关键词，跳过");
        return;
    }

    // ... 其余代码保持不变
}

// 3. 优化用户信息拉取错误处理
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
        catch (TL.RpcException ex) when (ex.ErrorCode == 400)
        {
            // 优化：特定错误处理，减少日志噪音
            _logger.LogDebug("无法获取用户信息，可能是隐私设置或用户限制");
        }
        catch (Exception ex)
        {
            // 优化：使用 Error 级别记录真正的错误
            _logger.LogError(ex, "获取用户信息时发生未预期错误");
        }
    }
    // ... 其余代码保持不变
}

// 4. 添加隐私保护的日志格式化
public static class SafeLoggerExtensions
{
    public static void LogMessageSafe(this ILogger logger, string message, LoggingOptions options)
    {
        if (!options.LogMessageContent)
        {
            message = message.Length > options.MaxMessageLength 
                ? $"[消息长度:{message.Length}]" 
                : "[消息内容已隐藏]";
        }
        
        logger.LogInformation("处理消息: {Message}", message);
    }
    
    public static void LogUserSafe(this ILogger logger, long userId, LoggingOptions options)
    {
        if (!options.LogUserIds)
        {
            logger.LogInformation("处理用户消息");
        }
        else
        {
            logger.LogInformation("处理用户 {UserId} 的消息", userId);
        }
    }
}