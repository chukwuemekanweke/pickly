# Payment Observability Specification

This document defines the current payment observability contract implemented in source and surfaced through the provisioned Grafana dashboards.

It is intentionally scoped to the payment flows that already exist today:

- payment initiation
- webhook receipt and persistence
- payment reconciliation
- wallet top-up fulfillment
- subscription activation fulfillment

## Goals

The payment observability model should answer three questions:

1. Are payment attempts being created?
2. Are provider callbacks and reconciliation moving transactions toward terminal state?
3. Are successful payments actually being fulfilled for each payment intent?

## Current Payment Event Taxonomy

Current payment business events:

- `payment.initiated`
- `payment.webhook.received`
- `payment.webhook.persisted`
- `payment.webhook.persistence_failed`
- `payment.reconciliation.confirmed`
- `payment.reconciliation.failed`
- `payment.credit_wallet`
- `payment.wallet.created`
- `payment.wallet.credited`
- `payment.activate_subscription`
- `payment.subscription.activated`

## Standard Payment Properties

Current payment event properties used by dashboards:

- `flow.id`
- `correlation_id`
- `tenant_id`
- `stakeholder_id`
- `provider`
- `payment_reference`
- `merchant_reference`
- `provider_reference`
- `payment_method`
- `payment_intent`
- `currency_id`
- `currency_code`
- `wallet_id`
- `terminal_state`
- `failure_reason`

## Dashboard Plan

The current payment scope uses three dashboards.

### 1. Payments Overview

Purpose:

- show payment creation, webhook intake, reconciliation, and fulfillment activity at a glance

Primary panels:

- initiated payments
- persisted webhooks
- confirmed reconciliations
- fulfilled wallet top-ups
- fulfilled subscriptions
- payment route request rate
- payment route p95 latency

### 2. Payments Fulfillment

Purpose:

- show the business outcome rate for each payment intent

Primary panels:

- wallet top-up fulfilled percentage
- subscription fulfilled percentage
- wallet execution completion percentage
- subscription execution completion percentage
- wallet top-up fulfillment flow over time
- subscription fulfillment flow over time

### 3. Payments Operations

Purpose:

- highlight operational failure modes and route health

Primary panels:

- webhook persistence failures
- reconciliation failures
- payment route 5xx rate
- webhook 401 rate
- webhook failure reasons
- reconciliation failure terminal states
- payment event volume by provider
- payment route traffic by route

## Fulfillment Definitions

These are the current derived fulfillment metrics exposed in Grafana.

- Wallet top-up fulfilled % = `payment.wallet.credited / payment.initiated` where `payment_intent = WalletTopUp`
- Subscription fulfilled % = `payment.subscription.activated / payment.initiated` where `payment_intent = Subscription`
- Wallet execution completion % = `payment.wallet.credited / payment.credit_wallet`
- Subscription execution completion % = `payment.subscription.activated / payment.activate_subscription`

These formulas intentionally separate the intent-specific business outcome from the internal post-confirmation dispatch step.
