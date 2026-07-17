# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added

- Private admin command `/excluded` for listing excluded tournaments eligible for the next poll.
- Private admin command `/unexclude` for returning tournaments to the candidate pool without deleting exclusion history.
- `/poll` arguments for a specific target date and a single-tournament single-choice poll.
- Process-wide rate limiting of CHGK API requests to two per second, including retries.
- Tournament IDs are hidden from poll announcements and `/excluded`, while retained in `/options`.
- `/options` now shows only the primary tournament price to keep its messages compact.

## [0.1.0] - 2026-07-17

### Added

- Initial implementation of the Telegram tournament polling bot.
