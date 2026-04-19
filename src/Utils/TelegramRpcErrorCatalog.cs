namespace TelegramMonitor;

public static class TelegramRpcErrorCatalog
{
    private static readonly Lazy<IReadOnlyList<TelegramRpcErrorEntry>> Entries = new(LoadEntries);
    private static readonly Lazy<Dictionary<string, TelegramRpcErrorEntry>> Lookup = new(BuildLookup);

    public static IReadOnlyList<TelegramRpcErrorEntry> GetAll() => Entries.Value;

    public static TelegramRpcErrorEntry? Find(string? errorKey)
    {
        if (string.IsNullOrWhiteSpace(errorKey))
            return null;

        var normalizedKey = errorKey.Trim().ToUpperInvariant();
        if (Lookup.Value.TryGetValue(normalizedKey, out var entry))
            return entry;

        var normalizedOfficialKey = NormalizeLookupKey(normalizedKey, "%d");
        if (!string.IsNullOrWhiteSpace(normalizedOfficialKey) &&
            Lookup.Value.TryGetValue(normalizedOfficialKey, out entry))
        {
            return entry;
        }

        var normalizedWTelegramKey = NormalizeLookupKey(normalizedKey, "X");
        if (!string.IsNullOrWhiteSpace(normalizedWTelegramKey) &&
            Lookup.Value.TryGetValue(normalizedWTelegramKey, out entry))
        {
            return entry;
        }

        return null;
    }

    public static string GetUserMessage(string? errorKey, int? x = null)
    {
        var entry = Find(errorKey);
        if (entry == null)
            return BuildFallbackMessage(errorKey, x);

        return FormatMessage(entry.MessageZh, x);
    }

    public static string GetUserMessage(RpcException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        int? x = exception.X >= 0 ? exception.X : null;
        return GetUserMessage(exception.Message, x);
    }

    public static string GetExceptionMessage(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is RpcException rpcException
            ? GetUserMessage(rpcException)
            : exception.Message;
    }

    public static string NormalizeOfficialKey(string? errorKey, int? x = null)
    {
        if (string.IsNullOrWhiteSpace(errorKey))
            return string.Empty;

        var normalizedKey = errorKey.Trim().ToUpperInvariant();
        if (normalizedKey.EndsWith("_X", StringComparison.Ordinal) && x.HasValue)
            return $"{normalizedKey[..^2]}_{x.Value}";

        return normalizedKey;
    }

    private static IReadOnlyList<TelegramRpcErrorEntry> LoadEntries()
    {
        var resourcePath = ResolveResourcePath();
        if (!File.Exists(resourcePath))
            return Array.Empty<TelegramRpcErrorEntry>();

        try
        {
            var json = File.ReadAllText(resourcePath);
            return JsonSerializer.Deserialize<List<TelegramRpcErrorEntry>>(
                       json,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new List<TelegramRpcErrorEntry>();
        }
        catch
        {
            return Array.Empty<TelegramRpcErrorEntry>();
        }
    }

    private static Dictionary<string, TelegramRpcErrorEntry> BuildLookup()
    {
        var dictionary = new Dictionary<string, TelegramRpcErrorEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in Entries.Value)
        {
            if (!string.IsNullOrWhiteSpace(entry.OfficialKey))
                dictionary[entry.OfficialKey] = entry;

            if (!string.IsNullOrWhiteSpace(entry.WTelegramKey))
                dictionary[entry.WTelegramKey] = entry;
        }

        return dictionary;
    }

    private static string ResolveResourcePath()
    {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "TelegramRpcErrors.zh-CN.json");
        if (File.Exists(runtimePath))
            return runtimePath;

        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "Resources",
                "TelegramRpcErrors.zh-CN.json"));
    }

    private static string NormalizeLookupKey(string errorKey, string replacementToken)
    {
        var tokens = errorKey.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return string.Empty;

        var hasNumericToken = false;
        for (var i = 0; i < tokens.Length; i++)
        {
            if (!int.TryParse(tokens[i], out _))
                continue;

            tokens[i] = replacementToken;
            hasNumericToken = true;
        }

        return hasNumericToken ? string.Join('_', tokens) : string.Empty;
    }

    private static string FormatMessage(string message, int? x) =>
        x.HasValue ? message.Replace("%d", x.Value.ToString()) : message;

    private static string BuildFallbackMessage(string? errorKey, int? x)
    {
        var normalizedKey = NormalizeOfficialKey(errorKey, x);
        return string.IsNullOrWhiteSpace(normalizedKey)
            ? "\u64cd\u4f5c\u5931\u8d25\uff0c\u8bf7\u7a0d\u540e\u91cd\u8bd5"
            : $"\u64cd\u4f5c\u5931\u8d25\uff0c\u8bf7\u7a0d\u540e\u91cd\u8bd5\uff08{normalizedKey}\uff09";
    }
}
