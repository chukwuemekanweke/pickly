---
type: infrastructure
title: Observability Implementation Conventions (OTel, Tail Sampling, Actor Identity)
description: Custom telemetry context, StakeholderId actor convention, and the local-tail-sampling / cloud-adaptive-sampling split for traces
resource: okf://knowledge/infrastructure/observability
tags:
    - constraint
    - infrastructure
    - observability
    - otel
    - tracing
governance: constraint
code_refs:
    - src/BLVK.Pickly.Logistics.Infrastructure/Observability/**
    - src/BLVK.Pickly.Logistics.Domain/Common/Observability/**
    - src/BLVK.Pickly.Logistics.Jobs/Observability/**
    - src/BLVK.Pickly.Logistics.WebAPI/Infrastructure/GlobalExceptionHandler.cs
    - src/BLVK.Pickly.Logistics.Consumer/BaseMessageHandler.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Messaging/RabbitMqOutboxMessageDispatcher.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Locations/GeocodingAttemptTelemetryHandler.cs
    - observability/otel-collector/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Infrastructure/Observability/CustomTelemetryContext.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Actor identity in telemetry

Use `StakeholderId` in custom observability events/spans for actor or subject identification whenever it can be resolved (via `CustomTelemetryContext`); fall back to `UserId`/`Email`/other identifiers
only when `StakeholderId` is genuinely unavailable. See [[dotnet-style]].

## Sampling architecture — do not add head sampling to application code

- Application code always emits complete telemetry. There is no sampling decision in app code, and none should be added there.
- Local development relies on an OTel Collector `tail_sampling` processor (`observability/otel-collector/otel-collector.yml`) that keeps error-status traces, slow traces
  (`OTEL_TAIL_SAMPLING_LATENCY_MS`), and a probabilistic fraction of the rest (`OTEL_TAIL_SAMPLING_PERCENTAGE`). This only affects the local traces pipeline; logs/metrics and cloud OTLP export are
  untouched.
- Cloud (Grafana Cloud) relies on Grafana's own Adaptive Traces sampling, configured outside this repo — do not attempt to replicate or override that from application code.
- For tail sampling to have something to key off, code paths that own an `Activity` and encounter a failure must mark it `ActivityStatusCode.Error` and record the exception — see
  `GlobalExceptionHandler` (WebAPI), `RabbitMqOutboxMessageDispatcher`, and `BaseMessageHandler` (Consumer) for the existing pattern. Any new top-level error boundary (a new global exception handler, a
  new base message-handler type) should follow the same pattern.

## Domain events and metrics

- Location resolution emits `Observability.EventNames.Locations.*` custom events (registry hit/miss, geocoding
  requested/failed, ambiguous, failed, registered/reused, duplicate-resolution prevented) via `ICustomTelemetryContext`.
- Infrastructure meters use `Observability.MeterName`, which `AddBackendTelemetry` registers (e.g.
  `pickly.locations.geocoding.requests` / `.duration`). Geocoding request metrics are recorded by an HTTP handler
  inside the retry policy, so they count every actual provider attempt, including retries, rather than one logical
  resolution operation. Metric tags must stay low-cardinality — never addresses, tokens or user identifiers.

## Constraint

Do not add probabilistic/head sampling logic in Application, WebAPI, Consumer, or Jobs code. If a new failure-handling boundary is added, mark its `Activity` as errored the same way the existing ones do, so local tail sampling keeps it.
