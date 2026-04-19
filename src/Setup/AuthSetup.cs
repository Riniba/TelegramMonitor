namespace TelegramMonitor;

public static class AuthSetup
{
    public static void AddAdminAuth(this IServiceCollection services)
    {
        var options = App.GetConfig<AuthOptions>("Auth") ?? new AuthOptions();
        ValidateOptions(options);

        services.AddHttpContextAccessor();
        services.AddAuthentication(authentication =>
            {
                authentication.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authentication.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authentication.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(cookie =>
            {
                cookie.Cookie.Name = string.IsNullOrWhiteSpace(options.CookieName)
                    ? "telegrammonitor_admin"
                    : options.CookieName.Trim();
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SameSite = SameSiteMode.Lax;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                cookie.ExpireTimeSpan = TimeSpan.FromMinutes(AdminAuthService.GetCookieExpireMinutes(options));
                cookie.SlidingExpiration = options.SlidingExpiration;
                cookie.LoginPath = "/api/auth/login";
                cookie.AccessDeniedPath = "/api/auth/me";
                cookie.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        services.AddSingleton<IAdminAuthService, AdminAuthService>();
    }

    private static void ValidateOptions(AuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AdminUsername))
            throw new InvalidOperationException("Auth 配置缺失: AdminUsername 不能为空");

        if (string.IsNullOrWhiteSpace(options.AdminPassword))
            throw new InvalidOperationException("Auth 配置缺失: AdminPassword 不能为空");
    }
}
