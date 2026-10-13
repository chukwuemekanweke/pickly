---
type: infrastructure
title: Local Dev Infrastructure Mode (Cloud-first Docker Compose)
description: docker-compose defaults to cloud-managed Postgres/Redis/RabbitMQ/Grafana Cloud; a Local profile restores the fully local stack
resource: okf://knowledge/infrastructure/dev-environment
tags:
    - constraint
    - infrastructure
    - docker-compose
    - dev-environment
governance: constraint
code_refs:
    - docker-compose.yml
    - docker-compose.local.yml
    - docker-compose.env.example.sh
sources:
    - kind: file
      path: docker-compose.yml
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Shape

- The default `docker-compose.yml` mode assumes managed cloud infrastructure (Neon PostgreSQL URLs, cloud Redis, cloud RabbitMQ, Grafana Cloud OTLP export for logs/metrics/traces) rather than starting every dependency in Docker.
- A single `local` Compose profile (services tagged `profiles: [local]`, layered via `docker-compose.local.yml`) restores the full local stack (local Postgres, Redis, RabbitMQ, the local
  Grafana/Loki/Tempo/Prometheus/OTel Collector stack used for [[observability]] tail sampling) for offline development.
- `docker-compose.env.example.sh` documents required credentials/URLs without committing real secrets.

## Constraint

- Do not commit real credentials, connection strings, or API keys into `docker-compose.yml`, `docker-compose.local.yml`, or any checked-in env file — only placeholders, with real values supplied via
  the untracked env file the example script documents.
- When adding a new infrastructure dependency, wire it into both the cloud-first default path and the `local` profile fallback; don't make a dependency local-only unless it has no cloud equivalent
  (e.g. the local observability stack itself).
