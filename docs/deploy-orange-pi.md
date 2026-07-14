# Деплой на Orange Pi

Деплой выполняется с рабочего Mac: скрипт синхронизирует исходный код на Orange Pi по SSH, а затем при необходимости собирает ARM64-образ и перезапускает Docker Compose непосредственно на устройстве. GitHub Actions и Tailscale credentials для этого способа не нужны.

`deploy-to-opi.sh` и `redeploy.sh` — локальные, намеренно игнорируемые Git файлы: они не публикуются вместе с исходным кодом.

## Подготовка Orange Pi

На Orange Pi должны быть установлены Docker и Docker Compose, а пользователь `deploy` должен входить в группу `docker`. Приложение размещается в `/opt/almaty-lis-polling-bot`.

Создайте конфигурацию только на Orange Pi:

```bash
cd /opt/almaty-lis-polling-bot
cp .env.example .env
cp secrets.env.example secrets.env
chmod 600 .env secrets.env
```

Заполните `TELEGRAM__BOTTOKEN`, `BOT__MAINADMINUSERID`, `BOT__TARGETCHATID` и остальные значения в `.env`. В `secrets.env` задайте одинаковый сильный пароль для `DATABASE__PASSWORD` и `POSTGRES_PASSWORD`.

## Деплой

На Mac должен быть настроен SSH skill `ssh-orangepi`: скрипт использует его `config.env`, автоматически выбирая локальный адрес Orange Pi, а при его недоступности — Tailscale-адрес.

```bash
# Скопировать обновлённый исходный код без перезапуска.
./deploy-to-opi.sh

# Скопировать, собрать ARM64-образ на Orange Pi и перезапустить сервисы.
./deploy-to-opi.sh --redeploy

# Полная пересборка без Docker cache.
./deploy-to-opi.sh --redeploy --no-cache
```

Скрипт никогда не передаёт `.env` и `secrets.env`, поэтому секреты остаются на Orange Pi. База PostgreSQL хранится в Docker volume и не удаляется при redeploy.

## Проверка и откат

```bash
cd /opt/almaty-lis-polling-bot
docker compose -f docker-compose.production.yml ps
docker compose -f docker-compose.production.yml logs --tail=100 bot

# Откат к образу до последнего redeploy.
BOT_IMAGE=almaty-lis-polling-bot:previous \
  docker compose -f docker-compose.production.yml up -d --force-recreate
```
