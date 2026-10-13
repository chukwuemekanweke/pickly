# Trace Sampling

This describes how trace retention is decided, split by environment. Application code is not involved in sampling decisions in either environment; it only emits complete, correctly tagged telemetry. Retention is an infrastructure concern.

## Ownership model

- WebAPI, Consumer, and Jobs always export every trace they produce. There is no head sampling (`TraceIdRatioBasedSampler`, `ParentBasedSampler`, or similar) in application code.
- Local development: the OpenTelemetry Collector (`observability/otel-collector/otel-collector.yml`) applies **tail sampling** before traces reach Tempo.
- Cloud (default `docker compose` mode): services export OTLP directly to the Grafana Cloud gateway, and **Adaptive Traces** (configured manually in the Grafana Cloud stack, not in this repository) decides retention before traces reach Tempo.

## Local tail sampling

The collector's `tail_sampling` processor sits in the traces pipeline only, ahead of `batch`:

```
otlp receiver -> tail_sampling -> batch -> otlp/tempo exporter
```

It evaluates three policies as OR conditions — a trace is kept if any one of them matches:

| Policy | Behavior | Environment variable | Default |
|---|---|---|---|
| `errors` | Keep any trace containing a span with status `ERROR`. | — | always on |
| `slow-traces` | Keep any trace whose duration exceeds the latency threshold. | `OTEL_TAIL_SAMPLING_LATENCY_MS` | `1000` |
| `normal-traffic` | Probabilistically keep a percentage of everything else. | `OTEL_TAIL_SAMPLING_PERCENTAGE` | `10` |

Both variables are read by the `otel-collector` service in `docker-compose.yml` and can be overridden in `.env` without editing the collector YAML — see `.env.example`. They only apply to the `local` profile; cloud mode does not start a local collector.

Logs and metrics are not affected: the `tail_sampling` processor is wired into the `traces` pipeline only, and the `logs`/metrics pipelines are unchanged.

`/health` and `/metrics` continue to be excluded at the WebAPI level (`AddAspNetCoreInstrumentation` filter in `ServiceCollectionExtensions.cs`), so those requests never produce spans in the first place. The collector does not add a second filter for them.

## Cloud sampling

Cloud mode keeps the existing direct-export architecture:

```
.NET services -> Grafana Cloud OTLP gateway -> Adaptive Traces -> Tempo
```

`OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_HEADERS`, and `OTEL_EXPORTER_OTLP_PROTOCOL` are unchanged. Grafana Cloud Adaptive Traces is configured manually in the Grafana Cloud stack (not in this repository) with:

- keep all traces containing `ERROR` spans
- keep traces slower than 1000 ms
- sample about 10% of normal traffic by volume
- keep unique trace fingerprints (a diversity policy local tail sampling does not replicate)

This repository does not provision or encode Adaptive Traces policy in code or `appsettings`.

## Error spans

Sampling reads span telemetry, not log severity, so handlers must mark spans correctly for the `errors` policy (and Adaptive Traces' equivalent) to work:

- `GlobalExceptionHandler` marks `Activity.Current` with `ActivityStatusCode.Error` and records the exception on the span before writing the friendly `ProblemDetails` response. `logger.LogError` alone does not affect sampling.
- `RabbitMqOutboxMessageDispatcher` and `BaseMessageHandler` (Consumer) mark their own `outbox_publish` / `{Message}_process` activities as `Error` and record the exception when dispatch or message handling fails, then rethrow so retry semantics are unchanged.

## Distributed trace continuity

`TraceId`, `correlation_id`, and `flow.id` remain distinct and are not merged:

- `TraceId` is the OpenTelemetry trace identifier, propagated via `Activity`.
- `correlation_id` is the request/message correlation identifier.
- `flow.id` is the customer-journey identifier (see `docs/observability/authentication-events.md`).

The existing propagation chain is unchanged: `OutboxWriter` stores `Activity.Current?.Id`, `RabbitMqOutboxMessageDispatcher` starts the `outbox_publish` producer activity parented by that stored ID (so it keeps the same `TraceId` as the original request), and the RabbitMQ publisher/subscriber `ActivitySource`s carry trace context over the wire to the Consumer's `BaseMessageHandler` activity.
