---
type: domain
title: Payments Domain
description: Generic payment transactions and providers, verified settlement, webhook idempotency, reconciliation, wallet credit, subscription activation, and delivery-payment routing
resource: okf://knowledge/domains/payments
tags:
    - context
    - domain
    - payments
    - wallet
    - webhooks
governance: context
code_refs:
    - src/BLVK.Pickly.Logistics.Application/Payments/**
    - src/BLVK.Pickly.Logistics.Domain/Payments/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Payments/**
    - src/BLVK.Pickly.Logistics.WebAPI/Features/Payments/**
    - src/BLVK.Pickly.Logistics.Jobs/Payments/**
    - src/BLVK.Pickly.Logistics.Contracts/Payments/**
    - src/BLVK.Pickly.Logistics.Contracts/Commands/Payments/**
    - tests/unit/BLVK.Pickly.Logistics.Application.UnitTests/Payments/**
    - tests/integration/BLVK.Pickly.Logistics.WebAPI.IntegrationTests/Payments/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Domain/Payments/Entities/Wallet.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Scope

- Payment providers: Credo and SafeHaven, each with their own webhook payload types under `Infrastructure/Payments/{Credo,SafeHaven}`. Provider activation/deactivation is a distinct feature
  (`ActivatePaymentProvider`) from initiating a payment.
- Wallet: `InitiatePayment`, wallet transactions, wallet top-up transaction detail, `CreditWallet` (triggered async, see below).
- Webhook inbox: incoming provider webhooks (`ProcessCredoWebhook`, `ProcessSafeHavenWebhook`, generic `ProcessPaymentWebhook`) are recorded in a `PaymentWebhookInbox` for idempotency before being
  acted on — do not process a webhook payload without first checking/recording it there.
- `ReconcilePayments` (Jobs) periodically reconciles payment state against provider truth.
- `SuccessfulPaymentConfirmed`, `ActivateSubscriptionCommand`, `CreditWalletCommand` cross into `Consumer`/`Jobs` via `Contracts` and the outbox — see [[async-processing]].

`PaymentIntent.DeliveryPayment` is a generic financial intent. `PaymentTransaction` deliberately has no `DeliveryId`
or other business-domain foreign key. It owns provider initiation state and the provider-verified settled amount,
currency and timestamp. The Deliveries domain owns attempt association through `DeliveryPaymentAttempt`.
Reconciliation publishes the existing `SuccessfulPaymentConfirmed` only after provider verification establishes the
settled amount and currency; delivery processing resolves the association by `PaymentTransactionId`.

Provider-reported success is not sufficient to satisfy any payment intent. Reconciliation requires an actual
provider-confirmed amount and currency that exactly match the requested `Amount` and `CurrencyId`. Missing or
mismatched evidence moves the transaction to the terminal `Unresolved` state, retains the available
`VerifiedAmount`, `VerifiedCurrencyId`/`VerifiedCurrencyCode`, timestamp and discrepancy reason, and publishes no
success event. Unresolved transactions are excluded from automatic reconciliation and require a future explicit,
auditable resolution workflow. Delivery-payment initiation treats them as outstanding so another charge cannot be
created while money may have moved. Its API reports `PaymentRequiresReview` (HTTP 409), never success or provider
instructions, for every request while any associated transaction is unresolved.

Delivery-payment idempotency keys are immutable operation identities. An exact-key retry returns that attempt's
persisted outcome, including distinct failed, expired and cancelled results, and is never redirected to another
outstanding transaction. Provider initialization runs under a second delivery-scoped transaction advisory lock and
re-reads the transaction after acquiring it; concurrent callers therefore cannot both initialize one reserved
transaction. The provider call and local `Initiated` transition intentionally occur while that lock is held. The
stable merchant reference supports investigation and provider-specific recovery, but is not assumed to make every
provider API retry-safe after a process loses a successful response; such ambiguous failures remain reconciliation
or manual-review concerns unless the provider offers lookup-by-reference semantics. Payment HTTP clients therefore
apply automatic transient retries only to `GET`/`HEAD`; provider initialization `POST` requests are never replayed by
the client policy.

Failed and expired delivery transactions remain reconciliation-eligible for late provider settlement. Only the
explicit delivery-only late-settlement transition may move those states to `Succeeded`, and it still requires an
exact amount/currency match. Wallet and subscription transactions retain ordinary terminal-state protections.

Follows the standard [[vertical-slices]] feature-folder shape and the [[unit-testing]]/[[integration-testing]] conventions. Distinct from the generic [[providers]] domain (email/file-storage provider registry).
