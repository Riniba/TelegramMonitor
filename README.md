# TelegramMonitor

TelegramMonitor is a web-based Telegram monitoring tool built on `WTelegramClient` and `Telegram.Bot`.

It provides a browser admin panel for:

- Telegram account login and monitoring
- Keyword rule management
- Archived message search
- Bot notification delivery
- Multi-account, long-running operation

## Features

- Web admin with cookie-based login
- Telegram account login flow with code and 2FA support
- Account-level monitoring start, stop, reconnect, and auto-recovery
- Keyword rules with `Exact`, `Contains`, `Regex`, and `Fuzzy` match modes
- Optional sender filtering by Telegram user ID or username
- `Monitor` and `Exclude` actions with priority support
- Archived message browsing and paging
- Telegram Bot notification targets with validation across all configured bots
- Default SQLite storage, with other SqlSugar-supported databases available

## Screenshots

### Login

![Login](./images/telegram1.png)

### Keyword Rules

![Keyword Rules](./images/keyword.png)

### Monitoring

![Monitoring](./images/telegram2.png)

### Runtime

![Runtime](./images/telegram3.png)

## Quick Start

### 1. Configure the app

For source-based development, edit `src/appsettings.json` or create `src/appsettings.Development.json`.

For release packages, edit the `appsettings.json` next to the executable, or use environment variables.

Minimum required configuration:

```json
{
  "Urls": "http://*:5005",
  "Telegram": {
    "DefaultApiId": 123456,
    "DefaultApiHash": "your_api_hash",
    "SessionsPath": "session"
  },
  "Auth": {
    "AdminUsername": "admin",
    "AdminPassword": "change-me"
  },
  "DbConnection": {
    "DbType": "Sqlite",
    "ConnectionString": "DataSource=telegrammonitor.db"
  },
  "Bot": {
    "Enabled": false,
    "Tokens": []
  }
}
```

Important notes:

- `Telegram.DefaultApiId` and `Telegram.DefaultApiHash` are required before starting Telegram account login.
- `Auth.AdminPassword` should always be changed before deployment.
- Do not commit real secrets, tokens, or production passwords to GitHub.

### 2. Run from source

```bash
dotnet build src/TelegramMonitor.csproj
dotnet run --project src/TelegramMonitor.csproj
```

Default URL:

```text
http://localhost:5005/
```

### 3. First-use flow

1. Open `/` and sign in with the configured admin account.
2. Go to `账号管理` (`/dashboard.html`).
3. Start Telegram login with phone number, then submit code and 2FA password if required.
4. Enable monitoring for the account.
5. Go to `关键词设置` (`/keywords.html`) and add rules.
6. Optionally configure Bot notifications in `Bot 通知` (`/bot.html`).

## Main Pages

- `/` - Admin login page
- `/dashboard.html` - Telegram accounts and monitoring
- `/keywords.html` - Keyword rules
- `/messages.html` - Archived messages
- `/bot.html` - Bot notification targets and bot status

## Bot Notification Notes

- Bot targets support both `Chat ID` and `@username`.
- A target is accepted only when all configured bots pass validation for that chat.
- For private chats, every configured bot must already have a valid conversation with the target user.

## Docker

Example container run:

```bash
docker run -d \
  --name telegram-monitor \
  --restart unless-stopped \
  -p 5005:5005 \
  -v ./tm-data:/data \
  -e Telegram__DefaultApiId=123456 \
  -e Telegram__DefaultApiHash=your_api_hash \
  -e Auth__AdminPassword=change-me \
  ghcr.io/riniba/telegrammonitor:latest
```

Container notes:

- Persistent runtime data is stored under `/data`.
- The image entrypoint links database files, sessions, and logs into `/data`.
- If Bot notifications are enabled, bot-side SQLite files are also persisted through `/data`.

## Environment Variables

Common overrides:

- `Urls`
- `Telegram__DefaultApiId`
- `Telegram__DefaultApiHash`
- `Telegram__SessionsPath`
- `Auth__AdminUsername`
- `Auth__AdminPassword`
- `DbConnection__DbType`
- `DbConnection__ConnectionString`
- `Bot__Enabled`
- `Bot__Tokens__0`
- `Bot__Tokens__1`

## Releases

- Latest release: https://github.com/Riniba/TelegramMonitor/releases/latest

## Documentation

- GitHub Wiki: https://github.com/Riniba/TelegramMonitor/wiki
- Wiki source prepared in this repository: [wiki/Home.md](./wiki/Home.md)
- Previous wiki content was used as the migration reference for the new pages

## License

This project is licensed under the [LICENSE](./LICENSE).
