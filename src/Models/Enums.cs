namespace TelegramMonitor;

public enum LoginState
{
    [Description("未登录")]
    NotLoggedIn = 0,

    [Description("等待验证码")]
    WaitingForVerificationCode = 1,

    [Description("等待密码")]
    WaitingForPassword = 2,

    [Description("等待姓名")]
    WaitingForName = 3,

    [Description("已登录")]
    LoggedIn = 4,

    [Description("其他")]
    Other = 5
}

public enum ProxyType
{
    [Description("跟随系统代理")]
    None = 0,

    [Description("SOCKS5")]
    Socks5 = 1,

    [Description("MTProxy")]
    MTProxy = 2
}
