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
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Riders/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Configurations/Rider*.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Configurations/DeliveryMatchingSessionConfiguration.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Configurations/DeliveryConfiguration.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Riders/**
    - src/BLVK.Pickly.Logistics.WebAPI/Features/Riders/**
    - src/BLVK.Pickly.Logistics.Jobs/Riders/**
    - tests/**/Riders/**
governance: constraint
---
## Code structure

Each rider endpoint is a vertical slice under `Application/Riders/Features/<OperationName>/` (`UpdateRiderLocation`,
`SetRiderAvailability`, `GetRiderStatus`, `ListRiderOffers`, `AcceptRiderOffer`, `RejectRiderOffer`,
`GetRiderActiveDelivery`); the Jobs matching loop calls `ProcessRiderMatchingHandler`. Handlers supply `TimeProvider`
time and `RiderMatchingOptions`, evaluates use-case rules, invokes aggregate transitions and returns its own status enum.
Purpose-specific ports in `Domain/Riders/Persistence/` expose tracked data and bounded query facts; sender delivery
retrieval uses `ISenderDeliveryAssignmentReader`. Implementations live in `Infrastructure/Persistence/Riders/`.
`RiderTransactionFactory` translates database conflicts and preserves isolation, `RiderConsistencyLock` owns stable
PostgreSQL advisory-lock ordering, and `RiderGeographyQueries` owns shared PostGIS primitives. Infrastructure never
selects rider application statuses or performs offer/availability/matching state transitions. There is no rider-wide
application service or store.

Rider actor lookup, offer lookup and offer rejection are ordinary aggregate access and therefore use
`IRepository<Stakeholder>` / `IRepository<RiderProfile>` / `IRepository<RiderDeliveryOffer>` /
`IRepository<DeliveryMatchingSession>` with rider specifications. The remaining specialized ports are retained because they provide PostGIS/shadow-property
access, atomic multi-aggregate state, bounded matching scans, or privacy-shaped projections; they are not one-to-one
wrappers created merely to mirror handlers.

## Identity, profile and operational state

`RiderProfile` is rider-only operational data associated one-to-one with an existing `Stakeholder` and scoped to the
same tenant. A usable rider must have a verified Stakeholder whose StakeholderType key is `rider`, an approved profile,
and an exact active `VehicleCategoryId`; the generic Stakeholder is not extended with dispatch state. There is no
rider onboarding API and no application path that marks a Stakeholder verified or assigns the `rider` type: the
`rider` StakeholderType is an automatic post-deploy seed, and staging test riders are registered through the normal
signup/email-confirmation API and then converted by the staging-only SQL fixture script, which mirrors
`RiderProfile.Create`/`Approve`/`ChangeVehicleCategory` and `Stakeholder.MarkVerified` (see [[database-seeding]]).

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
scan batches are required `Riders:Matching` configuration. Transaction retry count and base delay are also bounded
configuration. Phase 6 fixes assignment capacity at one and rejects another configured value. All time decisions use
injected `TimeProvider`.

One matching iteration runs three bounded read-committed scans (expired offers, due scheduled dispatches, and due
deliveries: no active session in backoff and no pending offer, so waiting deliveries cannot starve newer ones), then
handles each item in its own short transaction. Committed items survive a later item's failure; any non-cancellation
per-item failure is logged, counted in `FailedOperations`, and the batch continues.

## Atomic acceptance and privacy

Acceptance derives the rider from `ActorContext`, re-reads the offer/profile/location/session/Delivery, rechecks expiry,
approval, availability, freshness, exact vehicle, service zone, radius, Delivery matching eligibility and capacity,
then calls `Delivery.AssignRider`. PostgreSQL advisory locks for the Delivery and rider are acquired in stable order
inside a read-committed transaction that also row-locks the Delivery (`FOR UPDATE`) before loading it. That transaction accepts the offer, appends Delivery status history, completes the
session, revokes conflicts, and commits the assignment together. A filtered unique Delivery index for active statuses is
the final one-active-delivery-per-rider safeguard. The same winning acceptance is replay-safe; stale, losing, expired or
foreign-rider acceptance returns a conflict/forbidden result.

Offer listing exposes only decision-useful pickup and destination-area details before assignment. After assignment the
rider active-delivery endpoint includes recipient contact and fulfillment-facing snapshots. Sender delivery retrieval
adds matching state and the assigned rider's public name/avatar, never candidate rankings or GPS positions.

## Concurrency and locking

Locks are transaction-scoped `pg_advisory_xact_lock` keys `delivery:{id}` and `rider:{stakeholderId}`. Order is always
delivery locks (ascending id) then the rider lock; nothing takes a delivery lock after a rider lock, so there are no lock
cycles. There is no matching-processor-wide lock, so Jobs instances scale horizontally.

| Operation | Locks | Isolation | Mutates |
| --- | --- | --- | --- |
| Match delivery (Jobs) | delivery via *try* lock (skip if held) → candidate rider | read committed | session, new offer |
| Expire offer (Jobs) | delivery → rider | read committed | offer, session backoff |
| Open scheduled dispatch (Jobs) | delivery | read committed | delivery status + history |
| Accept offer | delivery → rider, then Delivery row `FOR UPDATE` | read committed | offer, delivery assignment + history, session, conflicting offers |
| Reject offer | delivery → rider | read committed | offer, session backoff |
| Set availability | pending-offer deliveries → rider | read committed | profile; offline also revokes offers + session backoff |
| Update location | rider | read committed | location |
| List offers | none (no transaction) | — | nothing; timed-out offers are filtered out |

The delivery lock protects that delivery's active session, its pending offer, and its assignment; the rider lock
protects availability, location, the rider's offer statuses, one pending offer per rider, and assignment capacity.
Going offline reads its pending offers' deliveries before locking; if the offer set grew before the rider lock was held
it rolls back and retries with the complete lock set (bounded, then `TemporarilyUnavailable`). Filtered unique indexes
remain the final boundary: `IX_DeliveryMatchingSessions_DeliveryId`, `IX_RiderDeliveryOffers_DeliveryId`,
`IX_RiderDeliveryOffers_RiderProfileId`, and `IX_Deliveries_RiderId`.

Read committed is the default deliberately: PostgreSQL takes a serializable snapshot at the transaction's first
statement, which is the lock call, so serializable reads after a lock wait can miss what the previous holder committed
(for example, matching offering a rider who just went offline). Acceptance also updates the Delivery, which other
modules (payments, cancellation) write without rider advisory locks, so it row-locks the Delivery before reading it:
the read observes their latest commit and they wait until the assignment commits. Contending acceptances therefore
queue on locks and read current state instead of failing with `40001` and retrying.

Any offer that leaves Pending without an assignment (Jobs expiry, rejection, acceptance of a timed-out offer, revocation
when going offline) goes through `RiderOfferRelease`, which also backs off the active session by `RetryBackoff`. Offer
listing never transitions offers; it hides timed-out ones until one of those paths runs.

Contention: blocking locks wait; matching skips a busy delivery until the next poll. `40001`/`40P01` retry the whole
callback with a new transaction and cleared EF tracking, up to `MaximumTransactionRetryCount`, with jittered exponential
backoff; exhaustion maps to `TemporarilyUnavailable` (HTTP 503) or a counted Jobs failure. The named unique indexes map
to `RiderPersistenceConflictException` and a business conflict without retry. Unrelated database errors are not
reclassified. Acceptance is replay-safe, and an ambiguous commit is verified by re-reading the accepted offer and
assigned delivery before returning success.

## Deployment fixtures (Phase 6B)

Rider presence is never seeded: positions expire after `LocationFreshness`, so testers report location and enable
availability through the rider APIs. Post-deploy `0007.test-rider-accounts` creates MVP dummy rider accounts
`piklytech+rider1..18@gmail.com` in every environment (confirmed login, verified `rider` stakeholder, profile, zones):
eligible riders per Enugu zone plus wrong-vehicle, missing-service-zone, unapproved, offline, stale-location and
active-assignment cases. It never resets an existing account, skips (never fails on) accounts with sender activity,
tags rows `system:test-rider-fixture`, and is removed before launch with
`Diagnostics/remove-test-rider-accounts.sql` after deleting 0007. `DatabaseMigrator/Diagnostics/rider-matching-diagnostics.sql` evaluates every rider against a delivery in the
same rule order as `RiderMatchingPersistence` and reports the first failing rule. Production rider identities are
never seeded. See [[database-seeding]].

## Observability

The matching workflow emits bounded-cardinality counters for scheduled dispatches, attempts, no-candidate outcomes,
offer creation/expiry/acceptance/rejection, isolated processing failures, assignment conflicts and stale location
submissions. Jobs logs only summary counts and failures; transaction retries log operation/resource identifiers but no
coordinates, phone numbers or addresses.
