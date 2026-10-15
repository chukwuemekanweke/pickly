---
type: domain
title: Deliveries Domain Foundation
description: Delivery aggregate ownership, parcel and address snapshots, lifecycle transitions, payment gates, fulfillment evidence seams, and persistence boundaries
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
    - src/BLVK.Pickly.Logistics.Infrastructure/Migrations/*AddDeliveryDomain*
    - tests/unit/BLVK.Pickly.Logistics.Domain.UnitTests/Deliveries/**
    - tests/integration/BLVK.Pickly.Logistics.WebAPI.IntegrationTests/Deliveries/**
governance: constraint
---
## Aggregate boundary

`Delivery` is the aggregate root. It owns one or more `DeliveryItem` parcels and append-only
`DeliveryStatusHistory` entries. A single immutable customer-facing tracking ID identifies the whole
delivery; parcels do not have independent tracking or lifecycles. `TenantId` and `SenderId` remain on
the aggregate for future authorization scoping.

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
