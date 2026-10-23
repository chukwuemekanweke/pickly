---
type: domain
title: Operations Metrics and Dashboard
description: Public point-in-time and daily time-series counts of riders and deliveries, reconstructed from append-only history, plus the static operations dashboard page
resource: okf://knowledge/domains/operations-metrics
tags:
    - constraint
    - domain
    - operations
    - metrics
    - deliveries
    - riders
    - webapi
governance: constraint
code_refs:
    - src/BLVK.Pickly.Logistics.Domain/Operations/**
    - src/BLVK.Pickly.Logistics.Application/Operations/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/OperationsMetricsReader.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Interceptors/RiderAvailabilityHistoryInterceptor.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Configurations/RiderAvailabilityHistoryConfiguration.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Migrations/*AddRiderAvailabilityHistory*
    - src/BLVK.Pickly.Logistics.Domain/Riders/Entities/RiderAvailabilityHistory.cs
    - src/BLVK.Pickly.Logistics.WebAPI/Features/Operations/**
    - src/BLVK.Pickly.Logistics.WebAPI/Infrastructure/CurrentActorMiddleware.cs
    - tests/unit/BLVK.Pickly.Logistics.Application.UnitTests/Operations/**
    - tests/unit/BLVK.Pickly.Logistics.WebAPI.UnitTests/Features/Operations/**
    - tests/integration/BLVK.Pickly.Logistics.WebAPI.IntegrationTests/Operations/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Infrastructure/Persistence/OperationsMetricsReader.cs
generated:
    at: "2026-10-23T00:00:00Z"
    by: claude
status: stable
---
## Shape

Two read-only vertical slices under `Application/Operations/Features/`: `GetOperationsSnapshot` (one UTC day) and
`GetOperationsTimeSeries` (inclusive UTC day range, default last 14 days, max 366). Both depend on the narrow read port
`Domain/Operations/ReadModels/IOperationsMetricsReader`, implemented by `OperationsMetricsReader` on
`AppReadDbContext` with set-based raw SQL. The reader returns factual counts only (deliveries by status at an instant,
riders by availability × has-active-delivery, transitions per day, scheduled pickups per day); the handlers own the
grouping into operational categories and all date/boundary decisions using injected `TimeProvider`.

`OperationsMetricsController` exposes `GET api/v1/operations/metrics/snapshot?date=` and
`GET api/v1/operations/metrics/time-series?from=&to=`. They are deliberately `[AllowAnonymous]`, cross-tenant and
aggregate-only (no identities, addresses or positions), so `CurrentActorMiddleware` excludes
`/api/v1/operations/metrics` (and the `/operations` page) from the `X-Tenant-Id` requirement. They rely on the
anonymous global rate limiter.

The dashboard is a dependency-free HTML page, `WebAPI/Features/Operations/Dashboard/OperationsDashboard.html`, compiled
in as an `EmbeddedResource` (logical name `OperationsDashboard.html`) and served at `GET /operations` by
`OperationsDashboardEndpoints.MapOperationsDashboard`. There is deliberately no `wwwroot`/static-file middleware: static
web assets only auto-map in Development, so an embedded page behaves identically in tests, Docker and production. The
page calls the two endpoints same-origin and renders hand-built SVG charts; it must never need auth or a tenant header.

## Point-in-time semantics (constraint)

- All days are UTC. For a completed day the state instant is the day's last tick; for the current day it is now.
  Future days are rejected (`DateInFuture`, HTTP 400). Time series may extend into the future (scheduled pickups).
- Delivery state at T = latest `DeliveryStatusHistory` entry with `OccurredAtUtc <= T` (ties broken by `Id`). Pending =
  `AwaitingDispatch` + `PendingAssignment`; in progress = `RiderAssigned`/`PickedUp`/`InTransit`; awaiting payment is
  separate. Completed/cancelled/failed are transitions *during* the day, not cumulative state.
- Rider Busy = rider stakeholder is `Delivery.RiderId` of a delivery in an active status at T, regardless of the
  availability toggle. Available = persisted `Available` and not busy. Everything else (offline, paused, unapproved)
  is Unavailable. Riders count only if their profile was created at or before T.
- Rider availability at T comes from `riders.RiderAvailabilityHistory`: latest entry at or before T; else the earliest
  later entry's `PreviousStatus` (Offline when null); else the profile's current value (no change since tracking began).
  Rows exist only from the history migration onward; earlier availability is approximated by that fallback.
- Time-series rider counts are distinct `Delivery.RiderId` among deliveries that entered `Delivered` (or
  `RiderAssigned`) that day; scheduled counts are `Scheduled` deliveries by `ScheduledPickupAtUtc` day in any status.

## Availability history capture (constraint)

`RiderAvailabilityHistoryInterceptor` (write `AppDbContext` only) appends a `RiderAvailabilityHistory` row in the same
save/transaction whenever a tracked `RiderProfile` is added or its `AvailabilityStatus` actually changes. It is
registered before `AuditAndSoftDeleteInterceptor` so the new rows receive audit fields. Do not record availability
history in handlers. Paths that change `AvailabilityStatus` with raw SQL or `ExecuteUpdate` bypass it and must insert
history themselves. The table has a restrict FK to `RiderProfiles`; soft deletion is unaffected.

## Testing

Handler and controller unit tests substitute `IOperationsMetricsReader`. The PostgreSQL integration test uses a
random far-future UTC day and before/after deltas because the containers database is shared across test classes.
