# TelegramMonitor Wiki

TelegramMonitor 是一个基于 Web 后台的 Telegram 监听工具，支持账号登录、关键词规则、消息归档和 Bot 通知。

## 你可以从这里开始

- [配置说明](配置说明)
- [Windows 安装与使用](Windows-安装与使用)
- [Docker 部署指南](Docker-部署指南)
- [账号与监控使用说明](账号与监控使用说明)
- [关键词使用教程](关键词使用教程)
- [Bot 通知配置](Bot-通知配置)

## 当前版本的主要功能

- 管理员登录页和 Cookie 会话认证
- Telegram 账号登录流程
- 账号监听启动、停止、重连和自动恢复
- 关键词规则管理
- 消息归档和检索
- 多 Bot 通知发送
- 默认 SQLite，支持切换其他数据库

## 首次部署建议流程

1. 先看 [配置说明](配置说明)，准备好 `Telegram ApiId/ApiHash` 和后台密码。
2. 根据你的环境选择 [Windows 安装与使用](Windows-安装与使用) 或 [Docker 部署指南](Docker-部署指南)。
3. 启动后访问根路径 `/`，登录后台。
4. 在 `账号管理` 页面完成 Telegram 账号登录并开启监听。
5. 在 `关键词设置` 页面添加规则。
6. 如果需要通知转发，再看 [Bot 通知配置](Bot-通知配置)。

## 后台页面说明

- `/`：登录页
- `/dashboard.html`：账号管理和监听控制
- `/keywords.html`：关键词规则管理
- `/messages.html`：消息归档查询
- `/bot.html`：Bot 状态和通知目标管理

## 运行环境要求

- 运行节点必须可以访问 Telegram
- 默认监听端口是 `5005`
- 需要你自己的 Telegram `ApiId` 和 `ApiHash`
- 建议部署前修改默认管理员密码

## 相关链接

- GitHub 仓库主页：https://github.com/Riniba/TelegramMonitor
- Releases：https://github.com/Riniba/TelegramMonitor/releases/latest
- GitHub Wiki：https://github.com/Riniba/TelegramMonitor/wiki
