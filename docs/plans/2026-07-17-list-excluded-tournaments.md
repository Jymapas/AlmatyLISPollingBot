# План реализации: просмотр исключённых турниров

Дизайн: `docs/designs/2026-07-17-list-excluded-tournaments.md`

Задачи выполняются последовательно по требованию пользователя.

## Фаза 1 — Application

### Задача 1.1: Добавить сценарий списка исключённых турниров

- [x] Добавить `ExecuteExcludedAsync` в `ListTournamentOptionsService`.
- [x] Повторно использовать расчёт даты, порт чтения исключений и API-выборку турниров.
- [x] Оставить только `IsExcluded` кандидатов после обычного отбора.
- [x] Дополнить unit-тесты актуальными, неподходящими и пустыми исключениями.

**Файлы:** `src/AlmatyLISPollingBot.Application/Features/Polls/Options/ListTournamentOptionsService.cs`, `tests/AlmatyLISPollingBot.Application.Tests/Features/Polls/Options/ListTournamentOptionsServiceTests.cs`

## Фаза 2 — Telegram transport и документация

*Зависит от: Фазы 1, поскольку роутер вызывает новый сценарий.*

### Задача 2.1: Подключить приватную команду

- [x] Добавить константу команды `/excluded`.
- [x] Направить команду только из личной переписки авторизованного администратора.
- [x] Вывести HTML-страницы или нейтральный текст при пустом результате.
- [x] Не раскрывать пользователю детали ошибок CHGK API.

**Файлы:** `src/AlmatyLISPollingBot.Application/Contracts/Bot/BotCommands.cs`, `src/AlmatyLISPollingBot.Worker/Telegram/TelegramUpdateRouter.cs`

### Задача 2.2: Обновить продуктовую документацию

- [x] Описать доступ и поведение `/excluded` в `requirements.md`.
- [x] Добавить запись в раздел `Unreleased` changelog.

**Файлы:** `requirements.md`, `changelog.md`

## Фаза 3 — Проверка и коммит

*Зависит от: Фаз 1–2.*

### Задача 3.1: Проверить качество и безопасность

- [x] Запустить форматирование, сборку и весь набор тестов.
- [x] Проверить diff на утечки секретов, неавторизованный доступ и раскрытие ошибок.
- [x] Запустить проверку уязвимостей пакетов.
- [x] Закоммитить завершённую фичу отдельно от пользовательских изменений.

**Файлы:** все файлы фичи.
