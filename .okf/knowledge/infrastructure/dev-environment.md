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
    - docker-compose.env.example.ps1
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

## Database image and location provider settings

- Local and Testcontainers PostgreSQL use `postgis/postgis:16-3.5-alpine` because migrations require the PostGIS
  extension; cloud databases must have PostGIS available (see [[locations]]).
- Geoapify and LocationIQ are the default geocoding and routing providers. Configure `GEOAPIFY_API_KEY` and
  `LOCATIONIQ_API_KEY`; Compose maps each key to both its `Locations` geocoding section and `Deliveries:Routing`
  section. `MAPBOX_ACCESS_TOKEN` remains available for both Mapbox sections when Mapbox is explicitly enabled.
  Provider keys are placeholders in both example environment scripts and must come from the operator's authorized
  commercial accounts. Empty keys do not fail startup, but an operation reaching that provider returns a
  configuration/provider failure.
- Payment provider options (`Payments:Credo`, `Payments:SafeHaven`) are not wired in compose; sandbox values are
  supplied as WebAPI environment variables when prepaid flows are tested (see [[database-seeding]] runbook).

## Constraint

- Do not commit real credentials, connection strings, or API keys into `docker-compose.yml`, `docker-compose.local.yml`, or any checked-in env file — only placeholders, with real values supplied via
  the untracked env file the example script documents.
- When adding a new infrastructure dependency, wire it into both the cloud-first default path and the `local` profile fallback; don't make a dependency local-only unless it has no cloud equivalent
  (e.g. the local observability stack itself).
