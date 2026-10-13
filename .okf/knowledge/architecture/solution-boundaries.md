---
type: architecture
title: Solution Boundaries and Dependency Rules
description: DDD project boundaries between Domain/Application/Infrastructure/WebAPI/Consumer/Jobs/Contracts and what may depend on what
resource: okf://knowledge/architecture/solution-boundaries
tags:
    - constraint
    - architecture
    - ddd
    - dependency-rules
governance: constraint
code_refs:
    - src/BLVK.Pickly.Logistics.Domain/**
    - src/BLVK.Pickly.Logistics.Application/**
    - src/BLVK.Pickly.Logistics.Infrastructure/**
    - src/BLVK.Pickly.Logistics.WebAPI/**
    - src/BLVK.Pickly.Logistics.Consumer/**
    - src/BLVK.Pickly.Logistics.Jobs/**
    - src/BLVK.Pickly.Logistics.Contracts/**
    - src/BLVK.Pickly.Logistics.DatabaseMigrator/**
sources:
    - kind: file
      path: AGENTS.md
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Solution boundaries

- `Domain` — entities, value objects, domain interfaces (including infrastructure-backed contracts such as caching/storage/persistence interfaces), domain services, specifications. No external dependency implementations here.
- `Application` — use cases organized by feature (vertical slices). Depends on `Domain` only.
- `Infrastructure` — implementations of `Domain` interfaces: EF Core persistence, Redis caching, Cloudflare R2 storage, RabbitMQ messaging, external API clients (Credo, SafeHaven), observability wiring.
- `WebAPI` — HTTP host and controllers only. No business logic, no direct infrastructure dependency implementations.
- `Consumer` — async message subscriber host (handles `Contracts` events/commands from RabbitMQ).
- `Jobs` — scheduled/background job host (outbox dispatch processing, reconciliation, health checks).
- `Contracts` — the shared wire-format project: commands and events exchanged between WebAPI/Application, the outbox, and Consumer/Jobs. Referenced by Application, Infrastructure, Consumer, Jobs — never the other way around.
- `DatabaseMigrator` — migration and pre/post-deploy SQL script execution only.

## Dependency rules (hard constraints)

- Infrastructure concerns (EF Core, Redis, HTTP clients, Cloudflare R2, RabbitMQ) must live in `Infrastructure`, never in `Application` or `WebAPI`.
- Interfaces for infrastructure-backed behavior live in `Domain` (e.g. `IJsonCache`, `IObjectStorageService`), implementations live in `Infrastructure`.
- `Domain` abstractions should stay narrow and reflect only what `Application` currently needs — do not pre-build generic capability surfaces.
- `WebAPI` must not implement infrastructure directly; it calls into `Application`.

When adding a new capability, decide which project it belongs to using this table before writing code — do not default to the first project that "seems close enough."
