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

- Testcontainers back integration dependencies; shared container setup lives in the project fixture, per-test data setup/cleanup lives in the test class.
- WebAPI auth integration tests delete the authentication records for the email used by the scenario.
- Consumer and Jobs integration tests currently validate health endpoints and don't create application records, so cleanup there is normally limited to response/host disposal.
