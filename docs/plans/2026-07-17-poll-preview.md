# План реализации: предпросмотр состава опроса

Дизайн: `docs/designs/2026-07-17-poll-preview.md`

Задачи выполняются последовательно по требованию пользователя.

## Фаза 1 — Общая подготовка кандидатов

### Задача 1.1: Устранить расхождение `/poll` и preview

- [x] Извлечь расчёт даты, force/exclude и отбор кандидатов из `StartPollService` в общий сервис.
- [x] Вернуть структурированные исходы для просроченной даты, 10+ force-кандидатов и пустого списка.
- [x] Сохранить у `/poll` прежние публикацию, уведомления и удаление только включённых force-турниров.

**Файлы:** `src/AlmatyLISPollingBot.Application/Features/Polls/StartPoll/*`

## Фаза 2 — Команда предпросмотра

### Задача 2.1: Добавить read-only use case и Telegram route

- [x] Добавить `PreviewPollService`, результат и DI-регистрацию.
- [x] Добавить private-only `/preview` с теми же аргументами, что `/poll`.
- [x] Отправить заголовок-снимок и тот же HTML-список кандидатов без visible ID; не публиковать poll и не менять БД.

**Файлы:** `src/AlmatyLISPollingBot.Application/Features/Polls/Preview/*`, `src/AlmatyLISPollingBot.Application/Contracts/Bot/BotCommands.cs`, `src/AlmatyLISPollingBot.Worker/Telegram/TelegramUpdateRouter.cs`

## Фаза 3 — Тесты и документация

### Задача 3.1: Закрепить изоляцию и точность списка

- [x] Проверить force-first порядок, exclude, полный список цен и отсутствие удаления из force-очереди.
- [x] Проверить отказ при десяти force-кандидатах и просроченной дате до API-запроса.
- [x] Обновить requirements и changelog в `Unreleased`.

**Файлы:** `tests/AlmatyLISPollingBot.Application.Tests/Features/Polls/Preview/*`, `requirements.md`, `changelog.md`

## Фаза 4 — Проверка и коммит

### Задача 4.1: Проверить качество и безопасность

- [x] Запустить форматирование, сборку и полный набор тестов.
- [x] Проверить отсутствие write-побочных эффектов, private authorization и нейтральную обработку ошибок внешних API.
- [x] Запустить проверку уязвимостей NuGet.
- [ ] Закоммитить фичу изолированно от пользовательских изменений.

**Файлы:** все файлы фичи.
