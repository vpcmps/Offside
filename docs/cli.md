# CLI

*[Português](pt-BR/cli.md) · [Back to docs](README.md)*

`Offside.Tool` scaffolds error catalogs and installs agent skills so that coding assistants working in your repository know the Offside conventions.

## Install

```bash
dotnet tool install -g Offside.Tool
```

The tool targets `net8.0` and rolls forward to later majors, so a machine with only .NET 10 installed can run it.

## Usage

```
offside init [--dir <path>] [--force]
```

| Option | Effect |
|---|---|
| `--dir <path>` | Target project root. Defaults to the current directory. |
| `--force` | Overwrite existing files. Without it, existing files are left untouched. |

`offside --help` prints the same summary.

## What it writes

Nine skills, into each of the three agent directories:

```
.cursor/skills/offside-setup/              .agents/skills/offside-setup/              .claude/skills/offside-setup/
.cursor/skills/offside-domain/             .agents/skills/offside-domain/             .claude/skills/offside-domain/
.cursor/skills/offside-aspnet/             .agents/skills/offside-aspnet/             .claude/skills/offside-aspnet/
.cursor/skills/offside-mediatr/            .agents/skills/offside-mediatr/            .claude/skills/offside-mediatr/
.cursor/skills/offside-fluentvalidation/   .agents/skills/offside-fluentvalidation/   .claude/skills/offside-fluentvalidation/
.cursor/skills/offside-fastendpoint/       .agents/skills/offside-fastendpoint/       .claude/skills/offside-fastendpoint/
.cursor/skills/offside-implementation/     .agents/skills/offside-implementation/     .claude/skills/offside-implementation/
.cursor/skills/offside-refactoring/        .agents/skills/offside-refactoring/        .claude/skills/offside-refactoring/
.cursor/skills/offside-azure-app-configuration/  .agents/skills/offside-azure-app-configuration/  .claude/skills/offside-azure-app-configuration/
```

| Skill | Covers |
|---|---|
| `offside-setup` | Wiring Offside into an existing project: packages, catalogs, DI, layering |
| `offside-domain` | Factories, `Custom`, `ErrorCode`, `Result` rules, the escape hatch |
| `offside-aspnet` | Endpoint mapping, the response shape, severity, 500 sanitization |
| `offside-mediatr` | Registration, ordered notification publication, scoped collection, retries |
| `offside-fluentvalidation` | FluentValidation failures → `Error` / `Result` |
| `offside-fastendpoint` | `UseOffside`, `SendOffsideAsync`, `DontProduceOffside` |
| `offside-implementation` | End-to-end feature work with the selected Offside integrations |
| `offside-refactoring` | Incremental migration to Offside while preserving public behavior |
| `offside-azure-app-configuration` | Dynamic message catalogs and refresh through Azure App Configuration |

### Capability selection

The setup, implementation, and refactoring skills inspect the project and ask the user to confirm a modular profile before changing files:

- exactly one message source: local JSON, Azure App Configuration, or a custom `IErrorMessageResolver`;
- exactly one exposure: domain/application only, standard ASP.NET Core, or ASP.NET Core with FastEndpoints;
- optional FluentValidation integration.

The setup skill also offers optional MediatR domain notifications.

These axes are independent. For example, JSON + FastEndpoints, Azure without HTTP, Azure + standard ASP.NET Core, and Azure + FastEndpoints are all supported. Agents use an interactive selector when available and fall back to a numbered Markdown checklist.

Plus two catalog templates:

```
errors/errors.json         invariant (English)
errors/errors.pt-BR.json   Brazilian Portuguese
```

Every path written is echoed to stdout, followed by the package commands to run next.

## Safety

Without `--force`, existing files are **skipped, never overwritten** — running `offside init` twice is harmless, and it will not clobber a catalog you have already translated. `--force` overwrites everything it would otherwise skip, so re-run with it only when you want the shipped versions back.

The command exits `1` and prints to stderr when the skills cannot be found or the target cannot be written; otherwise `0`.

## Without the CLI

Nothing here is required. The tool is a convenience — you can write `errors/errors.json` by hand from the template in [Messages and cultures](messages.md) and skip the skills entirely.
