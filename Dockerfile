FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-bookworm-slim

WORKDIR /app

ARG BIN_NAME=TelegramMonitor
ARG TARGETARCH

ENV APP_HOME=/app \
    APP_DATA=/data \
    BIN_NAME=${BIN_NAME}

LABEL org.opencontainers.image.title="TelegramMonitor" \
      org.opencontainers.image.description="Web-based Telegram monitoring tool with keyword rules, message archive, and bot notifications" \
      org.opencontainers.image.source="https://github.com/Riniba/TelegramMonitor"

COPY out/linux-${TARGETARCH}/ /app/
COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh

RUN chmod +x /app/${BIN_NAME} /usr/local/bin/docker-entrypoint.sh \
    && mkdir -p /data

VOLUME ["/data"]

EXPOSE 5005

ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]
