# Contributing to Offside

Thank you for contributing to Offside. By participating, you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## Prerequisites

- .NET SDK 10.0.400 (installed through `global.json`)
- Git

## Development workflow

1. Create a branch from `master`.
2. Restore dependencies with `dotnet restore --locked-mode`.
3. Build and test with `dotnet build Offside.sln -c Release --no-restore` and `dotnet test Offside.sln -c Release --no-build --no-restore`.
4. Run `dotnet pack Offside.sln -c Release --no-build --no-restore -o artifacts` when changing package contents or metadata.
5. Open a focused pull request describing the change, tests run, and any compatibility impact.

## Documentation

- User-facing documentation lives in `docs/`. English is canonical; `docs/pt-BR/` mirrors it page for page.
- **Changing a page in `docs/` means updating its `docs/pt-BR/` counterpart in the same pull request.** The two directories must always hold the same set of filenames.
- Public API changes belong in the XML doc comments on the type or member. `docs/api-reference.md` mirrors those comments — keep both in step.
- `docs/superpowers/` holds internal specs and plans. It is historical; do not treat it as user documentation.
- The build runs with `TreatWarningsAsErrors`, so a malformed `///` comment fails CI.

## Pull requests

- Keep changes small and covered by tests when behavior changes.
- Use clear, imperative commit messages; Conventional Commits are preferred.
- Do not include generated `bin`, `obj`, or package artifacts.
- Discuss breaking API or package changes in an issue before implementation.

## Reporting issues

Use GitHub Issues for bugs and feature requests. Do not disclose security vulnerabilities in public issues; follow [SECURITY.md](SECURITY.md) instead.
