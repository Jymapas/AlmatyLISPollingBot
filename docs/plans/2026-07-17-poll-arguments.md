# План реализации: аргументы `/poll`

Дизайн: `docs/designs/2026-07-17-poll-arguments.md`

Задачи выполняются последовательно по требованию пользователя.

## Фаза 1 — Контракт команды и постоянное состояние

### Задача 1.1: Ввести request запуска опроса

- [x] Добавить `StartPollRequest`, parser и тип результата разбора.
- [x] Принять только `/poll`, `/poll 1`, `/poll dd.MM.yyyy` и `/poll dd.MM.yyyy 1`.
- [x] Централизовать допустимые количества и правила single-choice в `PollRules`.
- [x] Добавить `DesiredTournamentCount` в `PollSession`, EF model, миграцию и snapshot с default `2`.

**Файлы:** `src/AlmatyLISPollingBot.Application/Features/Polls/StartPoll/*`, `src/AlmatyLISPollingBot.Domain/Common/PollRules.cs`, `src/AlmatyLISPollingBot.Domain/Entities/PollSession.cs`, `src/AlmatyLISPollingBot.Infrastructure/Persistence/Migrations/*`

## Фаза 2 — Use case и Telegram

*Зависит от: Фазы 1, поскольку сценарий и роутер используют новый request.*

### Задача 2.1: Применить аргументы при публикации

- [x] Перегрузить `StartPollService` с request и сохранить обратную совместимость запуска без аргументов.
- [x] Отклонять целевую дату с уже прошедшим временем автоостановки до внешних вызовов.
- [x] Формировать вопрос, single-choice флаг и сессию из desired count.
- [x] Разобрать payload в `TelegramUpdateRouter`, показать подсказку при ошибке и отправить нейтральное сообщение при просроченной дате.

**Файлы:** `src/AlmatyLISPollingBot.Application/Features/Polls/StartPoll/StartPollService.cs`, `src/AlmatyLISPollingBot.Worker/Telegram/TelegramUpdateRouter.cs`

## Фаза 3 — Тесты и документация

*Зависит от: Фаз 1–2.*

### Задача 3.1: Закрепить публичное поведение

- [x] Добавить unit-тесты parser.
- [x] Дополнить тесты StartPollService явной датой, single-choice, сохранением count и просроченным stop time.
- [x] Обновить требования для синтаксиса, single-choice и автоостановки целевой даты.
- [x] Добавить changelog в `Unreleased`.

**Файлы:** `tests/AlmatyLISPollingBot.Application.Tests/Features/Polls/StartPoll/*`, `requirements.md`, `changelog.md`

## Фаза 4 — Проверка и коммит

*Зависит от: Фаз 1–3.*

### Задача 4.1: Проверить качество и безопасность

- [x] Запустить форматирование, сборку и полный набор тестов.
- [x] Проверить diff на неавторизованный запуск, невалидные даты, прошедшее close time и утечки данных.
- [x] Запустить проверку уязвимостей NuGet.
- [x] Закоммитить фичу изолированно от пользовательских изменений.

**Файлы:** все файлы фичи.
