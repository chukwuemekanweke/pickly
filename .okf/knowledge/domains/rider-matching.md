---
type: domain
title: Rider Availability and Delivery Matching
description: Rider profiles, live PostGIS positions, zone eligibility, bounded delivery offers, atomic assignment and scheduled dispatch recovery
resource: okf://knowledge/domains/rider-matching
tags:
    - constraint
    - domain
    - riders
    - deliveries
    - matching
    - postgis
    - concurrency
    - jobs
sources:
    - resource: ""
generated:
    at: "2026-10-18T00:00:00Z"
    by: codex
status: stable
code_refs:
    - src/BLVK.Pickly.Logistics.Domain/Riders/**
    - src/BLVK.Pickly.Logistics.Application/Riders/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/RiderOperationsStore.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Configurations/Rider*.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Configurations/DeliveryMatchingSessionConfiguration.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Configurations/DeliveryConfiguration.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Riders/**
    - src/BLVK.Pickly.Logistics.WebAPI/Features/Riders/**
    - src/BLVK.Pickly.Logistics.Jobs/Riders/**
    - tests/**/Riders/**
governance: constraint
---
## Identity, profile and operational state

`RiderProfile` is rider-only operational data associated one-to-one with an existing `Stakeholder` and scoped to the
same tenant. A usable rider must have a verified Stakeholder whose StakeholderType key is `rider`, an approved profile,
and an exact active `VehicleCategoryId`; the generic Stakeholder is not extended with dispatch state. Phase 6B may
provision test riders by creating the normal Identity user, verified Stakeholder and `rider` StakeholderType, then a
`RiderProfile`, approval, and `RiderServiceZone` records through these domain factories and normal EF persistence.

The persisted availability value expresses rider intent (`Offline`, `Available`, or `Paused`). `Busy` is derived from
an authoritative active Delivery in `RiderAssigned`, `PickedUp`, or `InTransit`; clients cannot set it. Enabling
availability requires approval, a fresh current position, and no active assignment. Disabling availability revokes a
pending offer but never unassigns an accepted Delivery. Changing vehicle category forces the profile offline; callers
must reject a change while an incompatible active assignment exists.

## Current position and geographic eligibility

`RiderCurrentLocation` stores one replaceable row per profile. Infrastructure owns its shadow `Position` column as
`geography(Point,4326)` with a GiST index. Longitude is X and latitude is Y. The authenticated location endpoint derives
the rider from `ActorContext`, validates coordinates and future skew, rejects out-of-order capture timestamps, and is
rate-limited per authenticated user. PostgreSQL rider-scoped advisory locks serialize concurrent heartbeats and
availability/acceptance decisions. Raw coordinates are neither canonical `Location` registry records nor logged or
returned to senders.

Candidate discovery uses `ST_DWithin`/`ST_Distance` translation over the geography column, so radius and ranking are in
metres. It starts from the Delivery's immutable pickup snapshot and the quote's immutable origin-zone identifier,
revalidates that the configured zone still covers the pickup, then requires a matching `RiderServiceZone`. Candidates
must share the sender's tenant, have exact vehicle-category equality, a fresh position, enabled availability, no active
assignment, and no pending reservation. Distance then profile id provides deterministic ordering. Matching never calls
Mapbox.

## Sessions, offers and retry orchestration

An active `DeliveryMatchingSession` is unique per Delivery. One pending `RiderDeliveryOffer` is allowed per Delivery and
per rider profile through filtered unique indexes. Offers are explicit persisted opportunities, not assignments. The
Jobs `RiderMatchingProcessor` runs bounded recovery scans that: open eligible scheduled dispatches through
`Delivery.OpenDispatch`, expire timed-out offers, discover all eligible unassigned deliveries regardless of how they
became eligible, and create the next bounded offer. This covers instant Pay On Delivery creation, verified instant
prepayment, scheduled dispatch, rejection/expiry, and recovery after process or message outages without relying on a
volatile queue or Redis lock. No candidate leaves the Delivery `PendingAssignment` and schedules configurable backoff;
it does not fail, cancel, or refund the Delivery.

Search radius, widening, candidate count, offer timeout, attempts per cycle, retry delay, freshness, future skew and
scan batches are required `Riders:Matching` configuration. Phase 6 fixes assignment capacity at one and rejects another
configured value. All time decisions use injected `TimeProvider`.

## Atomic acceptance and privacy

Acceptance derives the rider from `ActorContext`, re-reads the offer/profile/location/session/Delivery, rechecks expiry,
approval, availability, freshness, exact vehicle, service zone, radius, Delivery matching eligibility and capacity,
then calls `Delivery.AssignRider`. PostgreSQL advisory locks for the Delivery and rider are acquired in stable order
inside a serializable transaction. That transaction accepts the offer, appends Delivery status history, completes the
session, revokes conflicts, and commits the assignment together. A filtered unique Delivery index for active statuses is
the final one-active-delivery-per-rider safeguard. The same winning acceptance is replay-safe; stale, losing, expired or
foreign-rider acceptance returns a conflict/forbidden result.

Offer listing exposes only decision-useful pickup and destination-area details before assignment. After assignment the
rider active-delivery endpoint includes recipient contact and fulfillment-facing snapshots. Sender delivery retrieval
adds matching state and the assigned rider's public name/avatar, never candidate rankings or GPS positions.

## Phase 6B prerequisites

Phase 6B must provision normal tenant/country/currency records, a `rider` StakeholderType, verified rider users and
Stakeholders, approved RiderProfiles with exact active VehicleCategories, RiderServiceZones referencing approved
DeliveryZone polygons, and fresh simulated RiderCurrentLocations. It must also provide delivery pricing/zone/vehicle
configuration and environment-specific `Riders:Matching`, database, Redis, payment-sandbox and messaging settings.
Production rider identities, coordinates, zones and rates are deliberately not seeded by Phase 6.

## Observability

The matching workflow emits bounded-cardinality counters for scheduled dispatches, attempts, no-candidate outcomes,
offer creation/expiry/acceptance/rejection, assignment conflicts and stale location submissions. Jobs logs only summary
counts and failures; neither metrics nor logs include coordinates, phone numbers or addresses.
