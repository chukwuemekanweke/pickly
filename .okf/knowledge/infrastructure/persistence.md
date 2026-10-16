---
type: infrastructure
title: Persistence Conventions (EF Core, Repositories, Specifications)
description: How data access is structured — AppDbContext/AppReadDbContext split, repository and specification pattern, interceptors for audit/soft-delete and observability
resource: okf://knowledge/infrastructure/persistence
tags:
    - constraint
    - infrastructure
    - persistence
    - ef-core
    - postgres
governance: constraint
code_refs:
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/**
    - src/BLVK.Pickly.Logistics.Domain/Common/Persistence/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Infrastructure/Persistence/EfRepository.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Shape

- PostgreSQL via EF Core. `AppDbContext` (writes) and `AppReadDbContext` (read models) both derive from `AppDbContextBase`; entity configurations live under `Persistence/Configurations/` (one `IEntityTypeConfiguration<T>` per entity).
- Domain code depends on repository/specification abstractions defined in `Domain` (e.g. `IAggregateRoot`-based repositories, `SpecificationEvaluator`); `Infrastructure` provides
  `EfRepository`/`EfReadRepository` implementations. Do not query `DbContext` directly from Application — go through a repository or read-model repository.
- Cross-cutting persistence behavior (audit fields, soft delete, observability spans around commands) is implemented via `SaveChangesInterceptor`s (`AuditAndSoftDeleteInterceptor`,
  `ObservabilityCommandInterceptor`), not by repeating that logic in each handler.
- `CurrentActorAccessor` resolves the acting stakeholder for audit fields; use it instead of re-deriving the actor from `HttpContext` in Infrastructure/Application code.
- Read-only, denormalized query paths (e.g. `StakeholderReadModelRepository`, `WalletTransactionReadModelRepository`) go through `AppReadDbContext`, not the write context.

## PostgreSQL / PostGIS specifics

- Both contexts and the design-time `AppDbContextFactory` call `UseNpgsql(..., npgsql => npgsql.UseNetTopologySuite())`
  and the model declares the `postgis` extension; any `DbContextOptionsBuilder` built in tests must do the same.
- Spatial values stay out of Domain: map value objects to NetTopologySuite types with a value converter in
  Infrastructure (see `GeographicCoordinatesConverter` in [[locations]]).
- With Npgsql plugins enabled, `DbContext.Database.GetConnectionString()` omits the password; database bootstrap
  (`DatabaseInitializationExtensions`) therefore reads the configured `PostgresWrite` connection string.
- `IgnoreQueryFilters()` applies to the whole query, including joined sets — never use it to relax the soft-delete
  filter on just one side of a join.
- Indexes EF cannot express (e.g. GIN expression indexes) are added with `migrationBuilder.Sql` in the migration
  that introduces the table, with a matching drop in `Down`.

## Constraint

New entities need an explicit `IEntityTypeConfiguration<T>` — do not rely on EF Core conventions alone for anything with non-trivial constraints, indexes, or owned types. New aggregates need a
specification-based query path rather than ad hoc LINQ scattered across handlers.
