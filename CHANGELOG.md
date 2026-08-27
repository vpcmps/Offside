# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.6.0] - 2026-08-27

### Added

- `OffsideAspNetCoreOptions.LegacyGeneralErrorName` (default `"generalErrors"`) for the brownfield `errors[].name` of a field-less error.
- `OffsideAspNetCoreOptions.RecordMode` (`PerError` | `PrimaryErrorOnly`) so the HTTP pipeline can record one event per request failure instead of one per error.

### Changed

- With `LegacyAliases` on, a field-less error now writes `errors[].name` as `"generalErrors"` instead of omitting it. `Field` stays null.
- `technicalDetail` no longer copies the business `detail`. It echoes `debug` only (Unexpected + `ExposeExceptionDetails`).
- FastEndpoints `GeneralErrorsField` (`AddError`/`ThrowError` without a property) maps to `Field` null, so the alias emits `"generalErrors"` rather than `"GeneralErrors"`.
- Documented dependency floors as a decision: `Offside.Refit` requires Refit `8.0` or later (`[8.0.0,16.0.0)`; Refit 5.x is unsupported). `Offside.FastEndpoint` requires FastEndpoints `8.3` or later. The FastEndpoints guide no longer says 7.x.

## [0.5.0] - 2026-08-26

### Added

- `IDomainErrorRecorder`, `DomainErrorSeverity`, and `DomainErrorMessageFormat` now live in the core `Offside` package. `Result.RecordTo` / `Result<T>.RecordTo` sit on the result types. HTTP hosts that call `ToHttpResult` or `SendOffsideAsync` do not need `RecordTo` — the problem pipeline records when a recorder is registered.
- `DomainErrorSeverityMap.Library` (the previous default) and `.Operations` (refusals including 404/400 as Warning; Unexpected as Error).
- `ActivityFailurePolicy.ServerErrors` on OpenTelemetry: mark the span for Unexpected / ServiceUnavailable / Timeout only. Default remains `None`.
- `OffsideRefitOptions.InboundStatus`. The default, `CollapseClientErrors`, folds every 4xx kind into `ServiceUnavailable` after status or problem-body mapping. `Mirror` is the 0.4.0 behaviour, opt-in for a BFF or two Offside services speaking of the same resource.
- `OffsideAspNetCoreOptions.LegacyAliases` (`MessageReasonAndTechnicalDetail`) for brownfield `message` / `reason` / `name` / `technicalDetail` fields.
- `OffsideAspNetCoreOptions.TelemetryProperties` for extra HTTP dimensions such as an operation name. The pipeline always writes `HttpStatus`.
- `IncludeArgumentKeys` on both telemetry option types. `IncludeArguments = true` still sends every argument and ignores the list. Arguments never reach the counter.
- A one-time warning under category `Offside` when `EmitMetric` is on and the meter has no listener, asking for `AddMeter(OffsideTelemetry.MeterName)`.
- Bilingual Kusto cookbook under `docs/queries.md` and `docs/pt-BR/queries.md`.

### Changed

- Breaking (pre-1.0): `IDomainErrorRecorder`, `DomainErrorSeverity`, and `DomainErrorMessageFormat` leave the `Offside.OpenTelemetry` and `Offside.ApplicationInsights` namespaces. Import `Offside`.
- Breaking (pre-1.0): Refit no longer mirrors 4xx by default. A raw 404 or an Offside problem body with `NotFound` becomes `external_api.service_unavailable`. Set `InboundStatus = Mirror` to restore 0.4.0.
- Breaking (pre-1.0): `ToHttpResult(HttpContext)` / `ToActionResult` that resolve options from DI throw `InvalidOperationException` naming `AddOffsideAspNetCore` when the singleton is missing. They no longer fall back to `new OffsideAspNetCoreOptions()`.
- Overloads that take `bool exposeExceptionDetails` and build empty options are `[Obsolete]`. Pass `HttpContext` or an explicit options object.
- `LogUnexpected` defaults off when a recorder is registered, so a 500 is not logged twice. Set it to `true` to keep both.

## [0.4.0] - 2026-08-26

### Added

- Optional `Offside.OpenTelemetry` package: records `Error` and `Result` failures through OpenTelemetry primitives — a structured `ILogger` entry, an `offside.error` event on the activity in scope, and an `offside.errors` counter — for hosts instrumented with `Azure.Monitor.OpenTelemetry` or any OTLP exporter, where no `TelemetryClient` exists. It references no OpenTelemetry or Azure package itself. Severity is kept identical to `Offside.ApplicationInsights`, enforced by a parity test. The counter carries only `offside.kind` and `offside.code`, to keep its cardinality bounded.
- Optional `Offside.OpenTelemetry.MediatR` package: the OpenTelemetry counterpart of the MediatR telemetry bridge.
- `OffsideApplicationInsightsOptions.FormatMessage` and `DomainErrorMessageFormat`, shaping the trace text from the error and its resolved message. `MessageOnly` (the default, and the previous behaviour), `CodePrefixed`, and `ErrorCodePrefixed` ship ready to use. It affects the trace text only, never the dimensions. `Offside.OpenTelemetry` offers the same formats under the same names.
- Bilingual OpenTelemetry guide under `docs/open-telemetry.md` and `docs/pt-BR/open-telemetry.md`.

## [0.3.0] - 2026-08-26

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

[Unreleased]: https://github.com/vpcmps/Offside/compare/v0.6.0...HEAD
[0.6.0]: https://github.com/vpcmps/Offside/compare/v0.5.0...v0.6.0
[0.5.0]: https://github.com/vpcmps/Offside/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/vpcmps/Offside/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/vpcmps/Offside/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/vpcmps/Offside/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/vpcmps/Offside/releases/tag/v0.1.0
