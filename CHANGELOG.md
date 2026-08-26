# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Optional `Offside.Testing` package: fluent assertions over `Result`, `Result<T>`, `Error`, and JSON message catalogs, with no test-framework dependency (failures throw `OffsideAssertionException`, which xUnit, NUnit, MSTest and TUnit all report as a failed test). Entry points are named `ShouldHaveError` rather than `Should()` so the package coexists with FluentAssertions and Shouldly. `OffsideCatalog` reads catalogs directly, making a missing code distinguishable from a template equal to the code, and detecting `{token}` values no argument fills.
- An `offside-testing` agent skill installed by `offside init` alongside the existing skills.
- Bilingual testing guide under `docs/testing.md` and `docs/pt-BR/testing.md`.
- Optional `Offside.Refit` package: maps a failed Refit call to Offside errors, mirroring the dependency's HTTP status onto `ErrorKind` and restoring an `application/problem+json` body when the dependency is itself an Offside service. Ships `IExternalApiCaller` (no `try`/`catch` at the call site), the `ApiException` mapping extensions, and `OffsideRefitDiagnosticsHandler` with an `IExternalApiErrorObserver` seam.
- Optional `Offside.ApplicationInsights` package: records `Error` and `Result` failures as Application Insights traces, with severity derived from `ErrorKind` and `offside.*` dimensions. `Error.Arguments` stay out of telemetry unless `IncludeArguments` is enabled.
- Optional `Offside.ApplicationInsights.MediatR` package: records published `DomainNotification` values as telemetry, alongside the existing scoped collector.
- Bilingual guides for both integrations under `docs/` and `docs/pt-BR/`.

### Changed

- The repository's own tests now assert results through `Offside.Testing` instead of `Assert.True(result.IsFailure)`.

## [0.2.0] - 2026-08-25

### Added

- `ErrorKind.ServiceUnavailable` (HTTP 503) and `ErrorKind.Timeout` (HTTP 504), with factories `Error.ServiceUnavailable` / `Error.Timeout`. Rank sits after `TooManyRequests` and before `Conflict`, so auth and rate-limit still win over a retryable outage.
- `OffsideProblem.Extensions` and `OffsideProblem.Item.Extensions` (`[JsonExtensionData]`) plus `OffsideAspNetCoreOptions.CustomizeProblem` for brownfield fields during deprecation.
- `OffsideAspNetCoreOptions.OnProblem` observability hook and `LogUnexpected` to suppress the built-in Unexpected log.
- `OffsideAspNetCoreOptions.ResolveTraceId`.
- `OffsideHttp.SelectPrimary` for custom writers.
- `OffsideOptions.AddJsonFile` and `AddJsonFromAssembly`, which fail at startup naming the missing file or resource.
- Optional `configure` callback on `AddOffsideAspNetCore`.
- Optional `Offside.MediatR` package with ordered domain-notification publication, a thread-safe scoped collector, and compatibility with MediatR 12 through 14.
- An `offside-mediatr` agent skill installed by `offside init` alongside the existing skills.
- Modular agent-skill workflows for Offside setup, feature implementation, refactoring, and Azure App Configuration. Interactive capability selection supports every valid combination of JSON, Azure or custom messages; domain-only, ASP.NET Core or FastEndpoints exposure; and optional FluentValidation.
- `Error.ErrorCode`: a stable screen identifier (`NOT_FOUND`, `ORDER_ALREADY_SHIPPED`) distinct from `Error.Code` (the message-catalog key). Factories take an optional trailing `errorCode`; blank uses `Error.DefaultErrorCode(Kind)`. Exposed on `OffsideProblem` as `errorCode` (document and `errors[]`). Sanitized 500s force `UNEXPECTED`.
- `Offside.FluentValidation`: maps FluentValidation failures to Offside `Error` / `Result`.
- `Offside.FastEndpoint`: `UseOffside`, `DontProduceOffside`, and `SendOffsideAsync` — Problem Details pipeline plus global expected-error OpenAPI metadata.
- `OffsideHttp.StatusCode` / `StatusCodes` for the kind → HTTP mapping.
- Bilingual documentation under `docs/` (English) and `docs/pt-BR/` (Portuguese): getting started, concepts, domain guide, ASP.NET Core guide, MediatR integration, FluentValidation, FastEndpoints, messages and cultures, CLI, API reference, and FAQ.
- XML documentation comments across the public API, so IntelliSense and the shipped `.xml` files are useful to consumers.

### Changed

- Breaking (pre-1.0): problem `traceId` is now `Activity.Current.TraceId` (32 hex, searchable as Application Insights `operation_Id`), not the W3C `Activity.Id` traceparent. Restore the previous format with `ResolveTraceId`.

## [0.1.0] - 2026-08-13

### Added

- Initial `Offside`, `Offside.AspNetCore`, and `Offside.Tool` packages.

[Unreleased]: https://github.com/vpcmps/Offside/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/vpcmps/Offside/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/vpcmps/Offside/releases/tag/v0.1.0
