# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

## [0.2.0] - 2026-07-20

### Added

- Private admin command `/excluded` for listing excluded tournaments eligible for the next poll.
- Private admin command `/unexclude` for returning tournaments to the candidate pool without deleting exclusion history.
- `/poll` arguments for a specific target date and a single-tournament single-choice poll.
- Process-wide rate limiting of CHGK API requests to two per second, including retries.
- Tournament IDs are hidden from poll announcements and `/excluded`, while retained in `/options`.
- `/options` now shows only the primary tournament price to keep its messages compact.
- Private admin command `/preview` for a side-effect-free preview of the next poll candidates, accounting for force and exclude.

### Changed

- CHGK date queries use the API's maximum page size to reduce pagination requests.

## [0.1.0] - 2026-07-17

### Added

- Initial implementation of the Telegram tournament polling bot.
