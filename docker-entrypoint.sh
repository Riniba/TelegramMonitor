#!/bin/sh
set -eu

APP_HOME="${APP_HOME:-/app}"
APP_DATA="${APP_DATA:-/data}"
BIN_NAME="${BIN_NAME:-TelegramMonitor}"

mkdir -p "$APP_DATA"

link_path() {
  source_path="$1"
  target_path="$2"
  target_dir="$(dirname "$target_path")"

  mkdir -p "$target_dir"

  if [ -L "$source_path" ]; then
    return
  fi

  if [ -e "$source_path" ]; then
    if [ ! -e "$target_path" ]; then
      mv "$source_path" "$target_path"
    else
      rm -rf "$source_path"
    fi
  fi

  ln -s "$target_path" "$source_path"
}

mkdir -p "$APP_DATA/session" "$APP_DATA/logs"

link_path "$APP_HOME/session" "$APP_DATA/session"
link_path "$APP_HOME/logs" "$APP_DATA/logs"

for name in \
  telegrammonitor.db telegrammonitor.db-journal telegrammonitor.db-shm telegrammonitor.db-wal \
  wtelegrambot.db wtelegrambot.db-journal wtelegrambot.db-shm wtelegrambot.db-wal
do
  link_path "$APP_HOME/$name" "$APP_DATA/$name"
done

i=0
while [ "$i" -lt 64 ]
do
  for suffix in "" "-journal" "-shm" "-wal"
  do
    name="wtelegrambot_${i}.db${suffix}"
    link_path "$APP_HOME/$name" "$APP_DATA/$name"
  done
  i=$((i + 1))
done

cd "$APP_HOME"
exec "$APP_HOME/$BIN_NAME" "$@"
