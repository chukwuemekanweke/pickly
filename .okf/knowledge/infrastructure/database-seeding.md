---
type: infrastructure
title: Database Seeding and Deployment SQL (DatabaseMigrator)
description: How post-deploy SQL is discovered and executed, rerun/atomicity rules for seed scripts, MVP test rider accounts, and the read-only Diagnostics folder
resource: okf://knowledge/infrastructure/database-seeding
tags:
    - constraint
    - infrastructure
    - persistence
    - postgres
    - seeding
    - deployment
governance: constraint
code_refs:
    - src/BLVK.Pickly.Logistics.DatabaseMigrator/Scripts/**
    - src/BLVK.Pickly.Logistics.DatabaseMigrator/Diagnostics/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/DatabaseInitializationExtensions.cs
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Infrastructure/Persistence/DatabaseInitializationExtensions.cs
    - kind: file
      path: src/BLVK.Pickly.Logistics.DatabaseMigrator/Diagnostics/README.md
generated:
    at: "2026-10-18T00:00:00Z"
    by: claude
status: stable
---
## Execution model

`DatabaseInitializationExtensions.InitializeDatabaseAsync` (DatabaseMigrator worker, or WebAPI when
`Database:InitializeOnStartup`) ensures the database exists, runs `Scripts/PreDeploy`, applies EF migrations, then runs
`Scripts/PostDeploy`. Only top-level `*.sql` files are read, in ordinal file-name order; each file is split on `GO`
lines and every batch goes through `ExecuteSqlRawAsync`.

- There is **no execution journal**: every PostDeploy script runs on every deployment.
- A batch (a whole file without `GO`) is one implicit transaction; a failure rolls the file back, marks the migrator
  unhealthy and stops later scripts. Temp tables and `DO $$` blocks work across the statements of one batch.
- Avoid `{`/`}` in post-deploy SQL (raw SQL passes through EF), psql meta-commands, and `GO` unless separate
  transactions are intended.

## Constraint: seed scripts must converge

- Key every row by a stable natural key backed by a unique index (`Code`, `(CountryCode, Code)`, `Version`,
  `(Provider, FeatureId)`, `(TenantId, Key)`), never by hard-coded generated GUIDs; resolve foreign keys through
  those keys. Use `INSERT ... ON CONFLICT (<cols>) WHERE "IsDeleted" = FALSE` for the filtered unique indexes.
- Declarative reference data (vehicle categories, zone geometry) may be realigned in place with
  `DO UPDATE ... WHERE <changed>`; operational state (zone activation/effective dates) is only set on insert.
- Financial versions are immutable: a pricing policy and all its rules are inserted together or not at all, and an
  existing version is never edited — tariff changes are new versions with their own effective period.
- Registry rows that other records reference (locations) are insert-only.
- Validate inputs (geometry validity/SRID/lon-lat order, matrix completeness, prerequisites) inside the script and
  `RAISE EXCEPTION` before writing, so nothing partial is ever activated. Take a `pg_advisory_xact_lock` per script.

## Current post-deploy seeds

0002 `rider` stakeholder type, 0003 vehicle categories, 0004 Enugu zones, 0005 Enugu OSM locations, 0006 ENU-V1
tariff, 0007 MVP test rider accounts, and 0008 the reviewed 51-landmark/616-street Enugu location expansion. All seed data lives in
`Scripts/PostDeploy`; there is no separate manual or environment-specific script path.

0007 is temporary dummy data that runs in every environment: it creates confirmed rider logins with a shared test
password hash (the password itself is never written in the repository), never resets an existing account, and skips
rather than fails on anything it cannot apply. Its rows are tagged `system:test-rider-fixture`; removal is deleting
0007 and running `Diagnostics/remove-test-rider-accounts.sql`. Fixture scripts never create payments or rider GPS
presence.

`DatabaseMigrator/Diagnostics/` holds read-only tooling that the migrator never runs and that is not in the build
output: `enugu-validation.sql` (PASS/WARN/FAIL report, non-zero exit on FAIL), `rider-matching-diagnostics.sql`, `remove-test-rider-accounts.sql`,
the `enugu-smoke.http` API collection, `GEOGRAPHIC-SOURCES.md` and the operator runbook `README.md`.
Related: [[persistence]], [[locations]], [[delivery-pricing-and-quotes]], [[rider-matching]], [[reference-data]].
