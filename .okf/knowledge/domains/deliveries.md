---
type: domain
title: Deliveries Domain Foundation
description: Delivery aggregate and sender-owned saved pickup locations, including address snapshots, lifecycle transitions, default-location consistency, and persistence boundaries
resource: okf://knowledge/domains/deliveries
tags:
    - constraint
    - domain
    - deliveries
    - fulfillment
    - payments
    - persistence
sources:
    - resource: ""
generated:
    at: "2026-10-15T00:00:00Z"
    by: codex
status: stable
code_refs:
    - src/BLVK.Pickly.Logistics.Domain/Deliveries/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Configurations/Delivery*.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Configurations/SavedPickupLocationConfiguration.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/*PickupLocation*.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Migrations/*AddDeliveryDomain*
    - src/BLVK.Pickly.Logistics.Infrastructure/Migrations/*AddSavedPickupLocations*
    - src/BLVK.Pickly.Logistics.Infrastructure/Migrations/*AddSharedLocationRegistry*
    - src/BLVK.Pickly.Logistics.Application/Deliveries/**
    - src/BLVK.Pickly.Logistics.WebAPI/Features/Deliveries/**
    - tests/unit/BLVK.Pickly.Logistics.Domain.UnitTests/Deliveries/**
    - tests/unit/BLVK.Pickly.Logistics.Application.UnitTests/Deliveries/**
    - tests/integration/BLVK.Pickly.Logistics.WebAPI.IntegrationTests/Deliveries/**
governance: constraint
---
## Aggregate boundary

`Delivery` is the aggregate root. It owns one or more `DeliveryItem` parcels and append-only
`DeliveryStatusHistory` entries. A single immutable customer-facing tracking ID identifies the whole
delivery; parcels do not have independent tracking or lifecycles. `SenderId` remains on the aggregate
for ownership scoping. Tenant membership is derived through the owning `Stakeholder`; duplicating
`TenantId` on Delivery would create another value that must be kept consistent with that relationship.

Pickup/drop-off addresses, recipient contact data, and the agreed charge are immutable snapshots.
They do not refer to mutable saved-location or recipient-account records. Delivery charges store the
existing Payments-domain `Currency` identifier; provider payment attempts remain owned by
`PaymentTransaction`, and Delivery stores only the trusted successful transaction reference.

## Creation and dispatch

- `Instant + PayBeforeDelivery` starts at `AwaitingPayment`.
- `Instant + PayOnDelivery` starts at `PendingAssignment`.
- `Scheduled + PayBeforeDelivery` starts at `AwaitingPayment`, then moves to `AwaitingDispatch` after
  verified payment.
- `Scheduled + PayOnDelivery` starts at `AwaitingDispatch`.

Scheduled creation records both the future pickup time and an explicit dispatch-eligibility time.
No lead-time policy is hardcoded. A future scheduler calls `OpenDispatch` at or after that timestamp;
only `PendingAssignment` deliveries with no assigned rider are matching-eligible.

## Payment and fulfillment invariants

Order type derives the only valid responsibility/method pair: sender/digital for Pay Before Delivery,
recipient/cash for Pay On Delivery. These are not independently mutable.

Trusted application workflows satisfy the delivery-level obligation by supplying references and
amount/currency evidence to `ConfirmVerifiedPrepaidPayment` or `ConfirmFullCashCollection`. Both
require the exact agreed amount and currency, and repeated confirmation with the same reference is
idempotent. A failed provider attempt returns prepaid delivery payment state to pending rather than
permanently failing the delivery.

The normal fulfillment sequence is `PendingAssignment -> RiderAssigned -> PickedUp -> InTransit ->
Delivered`. Completion requires an assigned rider, proof-of-delivery reference, and a fully satisfied
payment obligation. Pay On Delivery requires full cash collection during handover; cash remittance or
reconciliation is deliberately not a completion prerequisite.

Cancellation is limited to pre-pickup states. Delivery failure is limited to assigned/in-progress
fulfillment states. Delivered, failed, and cancelled states are terminal; no recovery, refund,
redelivery, return, or dispute policy is defined in this phase.

## Persistence and future boundaries

Delivery tables use the `deliveries` schema and the standard generic repository/specification,
auditing, and soft-delete infrastructure described in [[persistence]]. Tracking IDs and external
evidence references have database uniqueness constraints. Status history uses restricted deletion so
hard deletion of a delivery cannot silently cascade into its history.

Future phases own pricing/quote calculation, application commands and APIs, provider verification,
rider discovery, cash-collection records, proof generation, remittance/reconciliation, and read
models. They must call the aggregate's existing transition methods after validating their own
evidence; they must not introduce a generic status setter or treat delivery status as financial truth.

## Saved pickup locations

`SavedPickupLocation` is a separate aggregate in the Deliveries domain. It is a sender-owned association with a
shared registry `Location` (see [[locations]]): `SenderId`, required `LocationId` (FK, restrict delete), `Label`,
optional `AdditionalDirections` and `IsDefault`. It holds no canonical address or coordinates of its own, and
updating it never modifies the shared location. Its tenant is derived from the owning `Stakeholder`.

Create and update accept only an already-resolved `LocationId`; the handlers verify it exists and is usable and
otherwise return `LocationNotFound`/`LocationUnusable` (HTTP 400 with a `code`). They must never geocode, call a
provider, invoke location resolution or create a shared location — resolution is a separate, earlier step.
`Delivery` has no foreign key to a saved or shared location; a future booking workflow loads the authorized saved
location's shared `Location` and copies its address and coordinates into the immutable `DeliveryAddress` snapshot.

The first active location becomes the default. Create-with-default, set-default, and delete-default
operations run in one transaction and acquire a PostgreSQL transaction-scoped advisory lock keyed by
sender. A filtered unique index on `SenderId` where `IsDefault = TRUE` and `IsDeleted = FALSE` is the
final integrity safeguard. Setting the current default is a no-op. Deleting the default promotes the
oldest active location, ordered by `CreatedAtUtc` and then `Id`; deleting the last location leaves no
default.

All command and query paths derive sender ownership from `ActorContext.StakeholderId` and use
not-found semantics for records outside that scope. Authentication infrastructure remains responsible
for validating the actor's tenant membership. Lightweight reads use `AppReadDbContext` projections that join the shared location (no N+1); do not add
`IgnoreQueryFilters()` to that join — it is query-wide and would resurface soft-deleted saved locations.
Listing uses an opaque, feature-scoped cursor ordered by `CreatedAtUtc DESC, Id DESC`, fetches one
extra row, and deliberately does not sort the mutable default flag first so default changes cannot
duplicate or skip locations while paging.
