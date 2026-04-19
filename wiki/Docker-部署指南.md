# Docker 部署指南

当前 Docker 方案已经按容器环境重新整理：

- 运行镜像基于 .NET 10
- 持久化目录统一放到 `/data`
- 数据库、Bot SQLite、会话文件和日志会自动链接到 `/data`

## 快速开始

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

启动后访问：

```text
http://localhost:5005/
```

## 必填环境变量

- `Telegram__DefaultApiId`
- `Telegram__DefaultApiHash`
- `Auth__AdminPassword`

## 可选环境变量

- `Auth__AdminUsername`
- `Bot__Enabled`
- `Bot__Tokens__0`
- `Bot__Tokens__1`
- `DbConnection__DbType`
- `DbConnection__ConnectionString`
- `Urls`

## 数据持久化说明

容器内持久化目录：

```text
/data
```

会被自动持久化的内容：

- `telegrammonitor.db`
- `wtelegrambot*.db`
- `session/`
- `logs/`

所以不要再把整个 `/app` 作为卷挂载。

推荐：

```bash
-v ./tm-data:/data
```

不推荐：

```bash
-v ./anything:/app
```

## Docker Compose 示例

```yaml
services:
  telegram-monitor:
    image: ghcr.io/riniba/telegrammonitor:latest
    container_name: telegram-monitor
    restart: unless-stopped
    ports:
      - "5005:5005"
    volumes:
      - ./tm-data:/data
    environment:
      Telegram__DefaultApiId: "123456"
      Telegram__DefaultApiHash: "your_api_hash"
      Auth__AdminUsername: "admin"
      Auth__AdminPassword: "change-me"
      Bot__Enabled: "false"
```

启动：

```bash
docker compose up -d
```

## 常用命令

```bash
docker ps
docker logs telegram-monitor
docker logs -f telegram-monitor
docker restart telegram-monitor
docker stop telegram-monitor
docker rm -f telegram-monitor
```

## 故障排查

### 容器启动后页面打不开

- 先看 `docker logs telegram-monitor`
- 确认 `5005` 端口已映射
- 确认宿主机防火墙已放行端口

### Telegram 登录时报缺少配置

- 检查 `Telegram__DefaultApiId`
- 检查 `Telegram__DefaultApiHash`

### 数据没有持久化

- 确认卷挂载到了 `/data`
- 不要把整个 `/app` 目录覆盖掉
