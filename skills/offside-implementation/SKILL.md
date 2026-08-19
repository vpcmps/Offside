---
name: offside-implementation
description: Implements a .NET feature end to end using Offside Error and Result while respecting the project's selected catalogs, HTTP adapter, validation stack, and layer boundaries.
---

# Implement with Offside

Implement the requested behavior in the existing architecture; do not redesign unrelated code.

## Establish the integration profile

Inspect packages, DI, catalogs, endpoints, and tests. If the message source, exposure, and validation choices are not already confirmed for this task, use `offside-setup` and wait for the user's capability selection before changing files. Never infer authorization to install a detected integration.

State the confirmed profile, packages, and files expected to change.

## Implement the feature

- Model expected domain/application failures with `Error` and `Result` / `Result<T>`; reserve exceptions for unexpected failures.
- Keep `Offside.AspNetCore` and FastEndpoints types in the host. Domain and application projects reference `Offside` only, except for a deliberate application-boundary FluentValidation mapping.
- Use stable catalog keys for `Error.Code` and stable client screen identifiers for `Error.ErrorCode`.
- Add every new custom code to the selected message source. For local JSON, update the invariant catalog first and preserve translation tokens. For Azure, describe or apply the required configuration keys within the user's authorized environment.
- Map HTTP only through the selected adapter. Do not introduce FastEndpoints, FluentValidation, Azure, or JSON alongside a different confirmed choice.
- Preserve the repository's established response contracts unless the user explicitly requests a contract change.

## Verify the outcome

Add behavior-focused tests proportional to the change: domain results, validation aggregation, HTTP status/problem shape, or message resolution as applicable. Run the affected tests and build. Report the observable behavior, catalog additions, and any intentionally unchanged contracts.
