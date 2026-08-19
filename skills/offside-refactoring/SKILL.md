---
name: offside-refactoring
description: Refactors an existing .NET error-handling flow to Offside incrementally while preserving public signatures and observable HTTP behavior by default.
---

# Refactor to Offside

Migrate an existing flow in small, verifiable slices. Preserve public APIs, status codes, response payloads, and client identifiers unless the user explicitly authorizes a breaking change.

## Characterize and confirm

Inventory the target flow from domain rule through validation, exception handling, endpoint mapping, and message source. Record current signatures and observable outcomes. Add characterization tests where existing coverage does not protect those outcomes.

If the target message source, exposure, and validation choices are not already confirmed, use `offside-setup` and wait for the user's capability selection. Summarize the selected profile, packages, migration boundary, and expected files before editing.

## Migrate incrementally

- Start at an internal boundary: translate expected exceptions, null/sentinel returns, or ad hoc error DTOs into `Error` and `Result`.
- Propagate results outward one boundary at a time. Add temporary adapters when a public signature cannot change.
- Keep message wording in the selected catalog source and preserve existing client-facing codes where possible.
- Replace HTTP translation only after the application result path is stable. Use standard ASP.NET Core or FastEndpoints according to the confirmed exposure.
- Do not register JSON and Azure/custom resolvers together. Do not introduce optional packages outside the confirmed profile.
- Remove legacy handlers and adapters only after searches and tests show they have no remaining consumers.

Stop and request a decision if preserving behavior conflicts with correct Offside adoption, or if a public signature, status, payload, or screen identifier must change.

## Verify each slice

Run focused characterization and behavior tests after each coherent slice, then build and run the relevant suite. Compare before/after public behavior and report remaining legacy paths separately from completed migration work.
