# AGENTS.md

## Purpose

This repository is used to build `AlmatyLISPollingBot`, a Telegram bot on `C# 10` with clean architecture.
Before making changes, agents must read [requirements.md](/Users/jymapas/dev/AlmatyLISPollingBot/requirements.md).

## Working Rules

- Treat `requirements.md` as the source of truth for product scope.
- Prefer extending the existing architecture instead of introducing shortcuts.
- Keep the codebase ready for future bot commands in private chat, even if they are not implemented yet.
- Preserve clean architecture boundaries.
- Do not add business logic into transport or hosting layers.

## Implementation Expectations

- Use `C# 10`.
- Use `Telegram.Bot` for Telegram integration.
- Use `PostgreSQL` for persistence.
- Default bot runtime mode is `long polling`.
- Assume the main timezone is `Asia/Almaty` unless configuration says otherwise.
- Target runtime is a Docker container on `Orange Pi Zero 3`.
- Target host OS is `Ubuntu Server 24.04` (`Orange Pi 1.0.6 Noble`, `noble`).
- Prefer container and runtime choices that are compatible with ARM deployment.

## Change Discipline

- Make changes atomically.
- Group each commit around one coherent change.
- Do not mix refactoring, formatting, and feature work in one commit unless they are inseparable.
- Commit every new completed change as a separate commit when working on implementation tasks.
- Use `git conventional commits` for commit messages.

## Conventional Commit Format

Use messages in the form:

```text
type(scope): short summary
```

Examples:

```text
feat(application): add poll creation use case skeleton
fix(infrastructure): handle chgk api retry policy
chore(worker): wire health checks and configuration
test(application): add poll result calculation tests
docs(requirements): restructure project requirements
```

Recommended commit types:

- `feat`
- `fix`
- `refactor`
- `test`
- `docs`
- `chore`
- `ci`

## Safety Rules

- Do not rewrite or delete user changes unless explicitly requested.
- If the repository is dirty, isolate your changes and avoid touching unrelated files.
- Prefer small, reviewable diffs.
- If a requirement is ambiguous, update documentation first or leave a clear TODO in the appropriate layer.

## Documentation Rules

- Update `requirements.md` when product behavior changes.
- Keep documentation aligned with actual code structure.
- When introducing a new extension point, document its purpose briefly in code or docs.
