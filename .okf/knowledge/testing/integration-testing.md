---
type: testing
title: Integration Testing Conventions
description: Lifecycle, scope, naming and cleanup rules for integration tests across WebAPI, Consumer and Jobs, including Testcontainers usage
resource: okf://knowledge/testing/integration-testing
tags:
    - constraint
    - testing
    - integration-tests
governance: constraint
code_refs:
    - tests/integration/**
sources:
    - kind: file
      path: AGENTS.md
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Projects and scope

Separate integration test projects for `WebAPI`, `Consumer`, `Jobs` (plus `Observability` for collector/tracing infra checks). Integration tests cover the **happy path only** — one test per
endpoint/handler/background-service covering the most meaningful end-to-end path. Additional scenarios beyond the happy path are secondary, not the primary expectation.

## Layout

- Mirror the production feature-folder structure (see [[vertical-slices]]); one endpoint scenario per file.
- Multi-action controllers get endpoint-specific subfolders under the controller's feature folder; single-action controllers keep the test directly in the feature folder.
- Every WebAPI action, every Consumer handler, and every Jobs background service gets an integration test when added.

## Lifecycle

- Every integration test class implements `IAsyncLifetime`.
- `InitializeAsync`: seed data, create prerequisite records, create any SQL view/function/procedure the scenario needs.
- `DisposeAsync`: delete records created/touched by the test, clear test doubles/in-memory stores. Cleanup is explicit and targeted — never a vague global wipe when only a few records were touched.
- Use `Given / When / Then` structure only inside the test method (typically local anonymous functions); helper methods outside that structure use normal verb-based names.
- Naming follows the same `When_{ActionUnderTest}_With{ParametersOfTest}_Should` convention as unit tests where practical.

## Current conventions

- Container fixtures use `postgis/postgis:16-3.5-alpine` so migrations can enable PostGIS.
- `CustomWebApplicationFactory` replaces `ILocationGeocodingProvider` with `FakeLocationGeocodingProvider`; with no
  responder configured any geocoding call fails, so scenarios prove that flows such as pickup creation never geocode.
  Never call live paid providers from tests.
- `IntegrationSenderSeeder` creates and removes verified sender stakeholders for scenarios that need several senders.
- Location scenarios tag provider feature ids and address text with a per-run token so registry search, unique
  provider references and resolution keys never collide with other test classes sharing the container.

- Testcontainers back integration dependencies; shared container setup lives in the project fixture, per-test data setup/cleanup lives in the test class.
- WebAPI auth integration tests delete the authentication records for the email used by the scenario.
- Consumer and Jobs integration tests currently validate health endpoints and don't create application records, so cleanup there is normally limited to response/host disposal.
- Concurrency invariants are an explicit exception to the happy-path-only default: use separate DI scopes/DbContexts
  against the shared PostgreSQL container and assert persisted outcomes after all racers complete. Order races
  deterministically: hold the competing advisory lock in a separate scope's transaction and poll `pg_locks` for a
  non-granted advisory waiter instead of sleeping. Rider coverage includes concurrent processors and deliveries,
  duplicate acceptance, availability/acceptance and availability/matching races, expiry/acceptance, filtered unique
  constraints, forced PostgreSQL `40001` retry and retry exhaustion, and PostGIS eligibility cases.
- The shared container accumulates rows across the sequential collection, so scans in tests use generous batch sizes
  and generated short unique values (such as three-character currency codes) are checked for existence first.
