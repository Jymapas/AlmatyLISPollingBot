# План реализации: soft delete исключённых турниров

Дизайн: `docs/designs/2026-07-17-soft-delete-excluded-tournaments.md`

Задачи выполняются последовательно по требованию пользователя.

## Фаза 1 — Домен, порты и миграция

### Задача 1.1: Сохранить историю исключения

- [x] Добавить soft-delete поля в `ExcludedTournament` и конфигурацию EF.
- [x] Создать миграцию, оставляющую существующие записи активными.
- [x] Обновить snapshot EF.
- [x] Дополнить порт операциями реактивации и soft delete.

**Файлы:** `src/AlmatyLISPollingBot.Domain/Entities/ExcludedTournament.cs`, `src/AlmatyLISPollingBot.Infrastructure/Persistence/BotDbContext.cs`, `src/AlmatyLISPollingBot.Infrastructure/Persistence/Migrations/*`, `src/AlmatyLISPollingBot.Application/Abstractions/Persistence/IExcludedTournamentRepository.cs`

## Фаза 2 — Application и persistence

*Зависит от: Фазы 1, поскольку use case и репозиторий используют новую модель.*

### Задача 2.1: Реализовать обратимые операции исключения

- [x] Реактивировать soft-deleted строку при `/exclude` без создания дубликата.
- [x] Добавить `UnexcludeTournamentsService` и result-модель, повторно используя безопасный парсер ID.
- [x] Реализовать параметризованное атомарное soft delete в PostgreSQL-репозитории.
- [x] Скрыть soft-deleted строки в lookup-запросах.
- [x] Зарегистрировать use case в Application DI.

**Файлы:** `src/AlmatyLISPollingBot.Application/Features/ExcludedTournaments/*`, `src/AlmatyLISPollingBot.Infrastructure/Persistence/Repositories/ExcludedTournamentRepository.cs`, `src/AlmatyLISPollingBot.Infrastructure/Persistence/Repositories/LookupRepository.cs`, `src/AlmatyLISPollingBot.Application/DependencyInjection.cs`

## Фаза 3 — Telegram и тесты

*Зависит от: Фазы 2, поскольку роутер вызывает новый use case.*

### Задача 3.1: Подключить `/unexclude`

- [x] Добавить константу команды и состояние личного диалога.
- [x] Направить команду из личного чата авторизованного администратора.
- [x] Вывести подтверждение возврата и нейтральную пометку при ошибке CHGK API.
- [x] Документировать доступ и поведение команды.

**Файлы:** `src/AlmatyLISPollingBot.Application/Contracts/Bot/BotCommands.cs`, `src/AlmatyLISPollingBot.Worker/Telegram/PrivateAdminDialogKind.cs`, `src/AlmatyLISPollingBot.Worker/Telegram/TelegramUpdateRouter.cs`, `requirements.md`, `changelog.md`

### Задача 3.2: Покрыть сценарии тестами

- [x] Обновить тесты exclude для реактивации.
- [x] Добавить тесты unexclude: валидный ввод, невалидный ввод и ID уже в пуле.
- [x] Добавить проверку нового состояния диалога.
- [x] Запустить весь набор тестов.

**Файлы:** `tests/AlmatyLISPollingBot.Application.Tests/Features/ExcludedTournaments/*`, `tests/AlmatyLISPollingBot.Application.Tests/Telegram/InMemoryPrivateAdminDialogStateTests.cs`

## Фаза 4 — Проверка и коммит

*Зависит от: Фаз 1–3.*

### Задача 4.1: Проверить качество и безопасность

- [x] Запустить форматирование, сборку и полный набор тестов.
- [x] Проверить diff на hard delete, неавторизованный доступ, SQL-инъекции, утечки секретов и раскрытие ошибок.
- [x] Запустить проверку уязвимостей NuGet.
- [x] Закоммитить фичу изолированно от пользовательских изменений.

**Файлы:** все файлы фичи.
