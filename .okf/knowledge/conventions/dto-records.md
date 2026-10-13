---
type: convention
title: DTO and Record Conventions
description: Commands, requests, responses and results are records with positional syntax, not classes
resource: okf://knowledge/conventions/dto-records
tags:
    - constraint
    - conventions
    - dto
    - records
governance: constraint
code_refs:
    - src/BLVK.Pickly.Logistics.Application/**/Features/**
    - src/BLVK.Pickly.Logistics.WebAPI/Features/**
    - src/BLVK.Pickly.Logistics.Contracts/**
sources:
    - kind: file
      path: AGENTS.md
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Rule

Treat commands, requests, responses, results, and similar transport/application DTOs as `record` types by default, not `class`. Prefer positional record syntax:

```csharp
public sealed record SignUpCommand(string Email, string Password);
```

This applies to Application feature commands/responses/results, WebAPI request DTOs, and `Contracts` commands/events (see [[async-processing]]). Domain entities are not DTOs and follow normal entity
conventions instead (private constructors, factory methods) — this rule does not apply to them.
