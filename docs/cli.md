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

Four skills, into each of the three agent directories:

```
.cursor/skills/offside-setup/    .agents/skills/offside-setup/    .claude/skills/offside-setup/
.cursor/skills/offside-domain/   .agents/skills/offside-domain/   .claude/skills/offside-domain/
.cursor/skills/offside-aspnet/   .agents/skills/offside-aspnet/   .claude/skills/offside-aspnet/
.cursor/skills/offside-mediatr/  .agents/skills/offside-mediatr/  .claude/skills/offside-mediatr/
```

| Skill | Covers |
|---|---|
| `offside-setup` | Wiring Offside into an existing project: packages, catalogs, DI, layering |
| `offside-domain` | Factories, `Custom`, `Result` rules, the escape hatch |
| `offside-aspnet` | Endpoint mapping, the response shape, severity, 500 sanitization |
| `offside-mediatr` | Registration, ordered notification publication, scoped collection, retries |

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
