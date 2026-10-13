---
type: convention
title: .NET Coding Conventions (Time, GUIDs, CancellationToken, Usings)
description: Repository-wide C# style rules — TimeProvider, Guid.CreateVersion7, CancellationToken parameter shape, using-directive hygiene, one type per file, enum numbering
resource: okf://knowledge/conventions/dotnet-style
tags:
    - constraint
    - conventions
    - dotnet
    - style
governance: constraint
code_refs:
    - src/**/*.cs
    - tests/**/*.cs
sources:
    - kind: file
      path: AGENTS.md
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Rules

- Use `TimeProvider` for time-related behavior instead of `DateTime.UtcNow` directly, in application and test code.
- Use `Guid.CreateVersion7()` for generated GUIDs instead of `Guid.NewGuid()`.
- Method signatures use `CancellationToken cancellationToken` with no default value — never `= default`.
- Simplify type/namespace usage: no fully qualified type names when a `using` directive would work; order `using` directives alphabetically and remove unused ones from every file you touch.
- When multiple types share a name, use a `using` alias for the ambiguous type instead of leaving fully qualified names inline.
- Keep top-level types in separate files — never an interface and its implementation in the same `.cs` file.
- Enum values start at `1` unless an existing persisted enum in the repository already forces a different contract (do not change existing enum numbering).

## Identity and observability actor convention

- ASP.NET Core Identity is the authentication base — do not reintroduce custom authentication flows when built-in Identity behavior already covers the need. Keep any `UserManager` wrapper narrow:
  expose only the methods Application currently uses. See [[authentication]].
- Use `StakeholderId` in observability custom events for actor/subject identification whenever it can be resolved; fall back to `UserId`/`Email`/other identifiers only when `StakeholderId` is genuinely unavailable. See [[observability]].
