## Summary

Describe the change and its motivation.

## Validation

- [ ] `dotnet restore --locked-mode`
- [ ] `dotnet build Offside.sln -c Release --no-restore`
- [ ] `dotnet test Offside.sln -c Release --no-build --no-restore`
- [ ] README and docs (`docs/` + `docs/pt-BR/`) updated, or N/A (internal-only)

## Compatibility

- [ ] No public API, package, or behavior change.
- [ ] Compatibility impact described below.

<!-- Describe target-framework, package, or public API impact when applicable. -->
