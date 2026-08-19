# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Modular agent-skill workflows for Offside setup, feature implementation, refactoring, and Azure App Configuration. Interactive capability selection supports every valid combination of JSON, Azure or custom messages; domain-only, ASP.NET Core or FastEndpoints exposure; and optional FluentValidation.
- `Error.ErrorCode`: a stable screen identifier (`NOT_FOUND`, `ORDER_ALREADY_SHIPPED`) distinct from `Error.Code` (the message-catalog key). Factories take an optional trailing `errorCode`; blank uses `Error.DefaultErrorCode(Kind)`. Exposed on `OffsideProblem` as `errorCode` (document and `errors[]`). Sanitized 500s force `UNEXPECTED`.
- `Offside.FluentValidation`: maps FluentValidation failures to Offside `Error` / `Result`.
- `Offside.FastEndpoint`: `UseOffside`, `DontProduceOffside`, and `SendOffsideAsync` — Problem Details pipeline plus global expected-error OpenAPI metadata.
- `OffsideHttp.StatusCode` / `StatusCodes` for the kind → HTTP mapping.
- Bilingual documentation under `docs/` (English) and `docs/pt-BR/` (Portuguese): getting started, concepts, domain guide, ASP.NET Core guide, FluentValidation, FastEndpoints, messages and cultures, CLI, API reference, and FAQ.
- XML documentation comments across the public API, so IntelliSense and the shipped `.xml` files are useful to consumers.

## [0.1.0] - 2026-08-13

### Added

- Initial `Offside`, `Offside.AspNetCore`, and `Offside.Tool` packages.

[Unreleased]: https://github.com/vpcmps/Offside/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/vpcmps/Offside/releases/tag/v0.1.0
