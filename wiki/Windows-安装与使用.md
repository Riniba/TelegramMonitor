# Windows 安装与使用

本页适用于 Windows 10 / 11。

## 1. 下载程序

从 Releases 页面下载对应的 Windows 发布包：

- 64 位：`TelegramMonitor-windows-64.zip`
- 32 位：`TelegramMonitor-windows-32.zip`
- ARM64：`TelegramMonitor-windows-arm64.zip`

下载地址：

- https://github.com/Riniba/TelegramMonitor/releases/latest

解压到任意目录，例如：

```text
D:\TelegramMonitor\
```

## 2. 修改配置

编辑程序目录下的 `appsettings.json`，至少补齐以下内容：

- `Telegram.DefaultApiId`
- `Telegram.DefaultApiHash`
- `Auth.AdminPassword`

如果你使用 SQLite，可以保持数据库配置默认值：

```json
"DbConnection": {
  "DbType": "Sqlite",
  "ConnectionString": "DataSource=telegrammonitor.db"
}
```

默认端口：

```json
"Urls": "http://*:5005"
```

## 3. 启动程序

双击运行：

```text
TelegramMonitor.exe
```

如果一切正常，浏览器访问：

```text
http://localhost:5005/
```

## 4. 登录后台

1. 打开根路径 `/`
2. 输入 `Auth` 配置中的管理员账号和密码
3. 登录后进入账号管理页

## 5. 登录 Telegram 账号

在 `账号管理` 页面：

1. 输入手机号
2. 点击“发起登录”
3. 输入短信或 Telegram 验证码
4. 如有二步验证，再输入密码
5. 登录成功后启用监听

## 6. 配置关键词

进入 `关键词设置` 页面，添加规则后即可让监听消息参与匹配。

相关说明见：

- [关键词使用教程](关键词使用教程)

## 7. 可选：配置 Bot 通知

如果你需要把命中消息发到另一个 Telegram 聊天，继续看：

- [Bot 通知配置](Bot-通知配置)

## 常见问题

### 页面打不开

- 检查程序是否启动成功
- 检查端口是否被占用
- 检查 Windows 防火墙是否拦截

### 无法登录 Telegram

- 检查 `ApiId` 和 `ApiHash` 是否填写正确
- 检查当前网络是否可以访问 Telegram
- 尝试重新启动程序后重新登录
