# Деплой на Orange Pi через GitHub Actions

Этот проект разворачивается без Kubernetes и без container registry: GitHub Actions собирает ARM64-образ, через Tailscale передаёт его на Orange Pi по SSH и запускает через Docker Compose. PostgreSQL и файлы конфигурации остаются на Orange Pi. Порт SSH не открывается в интернет.

## Что понадобится

- Orange Pi Zero 3 с 64-битной Ubuntu Server 24.04, подключённый к Tailscale;
- репозиторий на GitHub и права на добавление Actions secrets;
- данные Telegram-бота и параметры из `.env.example`.

## 1. Подключиться к Orange Pi

На рабочем компьютере подключитесь к серверу под пользователем с `sudo`:

```bash
ssh <user>@<orange-pi-host>
```

## 2. Установить Docker

На Orange Pi выполните:

```bash
sudo apt update
sudo apt install -y ca-certificates curl
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
rm get-docker.sh
sudo usermod -aG docker "$USER"
newgrp docker
docker version
docker compose version
```

После `usermod` можно также выйти и снова войти по SSH. Далее все команды выполняйте от этого же пользователя без `sudo`.

## 3. Создать папку приложения в `/opt`

```bash
sudo mkdir -p /opt/almaty-lis-polling-bot
sudo chown -R "$USER":"$USER" /opt/almaty-lis-polling-bot
cd /opt/almaty-lis-polling-bot
```

## 4. Создать конфигурацию

Скопируйте в папку сервера шаблоны из репозитория либо создайте файлы вручную:

```bash
cd /opt/almaty-lis-polling-bot
nano .env
nano secrets.env
chmod 600 secrets.env
chmod 600 .env
```

Содержимое `.env` возьмите из [`.env.example`](../.env.example). Укажите как минимум `TELEGRAM__BOTTOKEN`, `BOT__MAINADMINUSERID`, `BOT__TARGETCHATID`, `BOT__DEFAULTVENUE` и параметры базы. Значение `DATABASE__HOST` в Docker Compose принудительно устанавливается в `postgres`; `DATABASE__PORT=5432` и `BOT__APPLICATIONTIMEZONE=Asia/Almaty` обычно менять не нужно.

В `secrets.env` создайте значения по [`secrets.env.example`](../secrets.env.example). `DATABASE__PASSWORD` и `POSTGRES_PASSWORD` должны быть одним и тем же сильным паролем. Этот файл не отправляется в GitHub.

## 5. Создать SSH-ключ для GitHub Actions

На своём компьютере создайте отдельный ключ без passphrase:

```bash
ssh-keygen -t ed25519 -f ~/.ssh/almaty-lis-polling-bot-github -C github-actions-almaty-lis-polling-bot
ssh-copy-id -i ~/.ssh/almaty-lis-polling-bot-github.pub <user>@<orange-pi-tailscale-host>
```

Проверьте вход:

```bash
ssh -i ~/.ssh/almaty-lis-polling-bot-github <user>@<orange-pi-tailscale-host>
```

## 6. Разрешить GitHub Actions доступ через Tailscale

В Tailscale Admin Console создайте OAuth client с правом `auth_keys` и тегом `tag:github-actions`. В tailnet policy разрешите этому тегу обращаться к Orange Pi на SSH-порт, например:

```json
{
  "action": "accept",
  "src": ["tag:github-actions"],
  "dst": ["orangepizero3:22"]
}
```

Скопируйте ID и secret созданного OAuth client: secret показывается только один раз. Для постоянной интеграции можно заменить OAuth client на workload identity federation по документации Tailscale.

## 7. Добавить GitHub Actions secrets

Откройте репозиторий: **Settings → Secrets and variables → Actions → New repository secret**. Добавьте:

| Secret | Значение |
| --- | --- |
| `ORANGE_PI_TAILSCALE_HOST` | Tailscale IP либо MagicDNS-имя Orange Pi |
| `ORANGE_PI_PORT` | SSH-порт, обычно `22` |
| `ORANGE_PI_USERNAME` | Пользователь Orange Pi, добавленный в группу `docker` |
| `ORANGE_PI_SSH_PRIVATE_KEY` | Полное содержимое `~/.ssh/almaty-lis-polling-bot-github` |
| `ORANGE_PI_DEPLOY_PATH` | `/opt/almaty-lis-polling-bot` |
| `TS_OAUTH_CLIENT_ID` | ID OAuth client Tailscale с тегом `tag:github-actions` |
| `TS_OAUTH_CLIENT_SECRET` | Secret этого OAuth client |

Не добавляйте в GitHub Telegram token и пароли PostgreSQL: они уже хранятся в `.env` и `secrets.env` на Orange Pi.

## 8. Отправить изменения в `main`

Workflow [`.github/workflows/deploy-orange-pi.yml`](../.github/workflows/deploy-orange-pi.yml) запускается при каждом push в `main`; его можно запустить вручную в GitHub: **Actions → Deploy to Orange Pi → Run workflow**.

Pipeline сначала запускает тесты, затем собирает Docker-образ `linux/arm64`, передаёт его на сервер, сохраняет прежний образ под тегом `almaty-lis-polling-bot:previous` и перезапускает контейнеры.

## 9. Проверить первый деплой

На Orange Pi выполните:

```bash
cd /opt/almaty-lis-polling-bot
docker compose -f docker-compose.production.yml ps
docker compose -f docker-compose.production.yml logs --tail=100 bot
```

Статус `running` у `bot` и `postgres` означает, что Compose запустил контейнеры. В логах не должно быть ошибок конфигурации или Telegram.

## 10. Откатить неудачный деплой

Если новый контейнер не работает, выполните на Orange Pi:

```bash
cd /opt/almaty-lis-polling-bot
BOT_IMAGE=almaty-lis-polling-bot:previous docker compose -f docker-compose.production.yml up -d --force-recreate
docker compose -f docker-compose.production.yml logs --tail=100 bot
```

Тег `previous` создаётся перед каждым новым развёртыванием. Если первый деплой не завершился, предыдущего образа ещё нет.

## Эксплуатация

Просмотреть логи: `docker compose -f docker-compose.production.yml logs -f bot`.

Остановить: `docker compose -f docker-compose.production.yml down`.

Обновить конфигурацию: отредактируйте `.env` или `secrets.env`, затем выполните `BOT_IMAGE=$(docker inspect --format '{{.Config.Image}}' almaty-lis-polling-bot) docker compose -f docker-compose.production.yml up -d`.
