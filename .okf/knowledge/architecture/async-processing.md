---
type: architecture
title: Async Processing Architecture (Contracts, Outbox, Consumer, Jobs)
description: How commands/events cross process boundaries via the Contracts project, the transactional outbox, RabbitMQ, and the Consumer/Jobs hosts
resource: okf://knowledge/architecture/async-processing
tags:
    - constraint
    - architecture
    - messaging
    - async
governance: constraint
code_refs:
    - src/BLVK.Pickly.Logistics.Contracts/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Messaging/**
    - src/BLVK.Pickly.Logistics.Jobs/OutboxProcessing/**
    - src/BLVK.Pickly.Logistics.Consumer/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Infrastructure/Messaging/OutboxWriter.cs
    - kind: file
      path: src/BLVK.Pickly.Logistics.Jobs/OutboxProcessing/OutboxMessageProcessor.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Shape

- `Contracts` defines the commands and events that cross process boundaries (`BaseCommand`, `BaseEvent`, and concrete types like `UserCreated`, `SuccessfulPaymentConfirmed`, `SendNotificationCommand`).
  Application, Infrastructure, Consumer and Jobs all reference `Contracts`; `Contracts` references nothing else in the solution.
- Application handlers that need to trigger async work write to the transactional outbox (`IOutboxWriter`) in the same database transaction as their domain change — never publish directly to RabbitMQ from a request-handling path.
- `Jobs.OutboxProcessing.OutboxMessageProcessor` polls/listens (`OutboxNotificationListener`) for pending outbox rows and hands them to `RabbitMqOutboxMessageDispatcher`, which publishes to RabbitMQ and marks the row dispatched.
- `Consumer` hosts message handlers (subclassing `BaseMessageHandler`) that receive `Contracts` commands/events from RabbitMQ and perform the actual side effect (send email, credit wallet, activate subscription, etc).
  Inbound messages are deduplicated via a message inbox.

## Constraints

- Never publish to RabbitMQ directly from an Application/WebAPI request path; always go through the outbox so the write and the intent-to-publish commit atomically.
- New cross-process command/event types belong in `Contracts`, not defined ad hoc inside Application or Consumer.
- Consumer handlers must be idempotent-safe against redelivery (see the message inbox pattern already used) and must mark their `Activity` as errored on failure — see [[observability]].

For delivery prepayment, generic `SuccessfulPaymentConfirmed` remains unchanged. Its router emits
`ConfirmDeliveryPaymentCommand` through the command outbox, and the command consumer resolves the delivery-owned
`DeliveryPaymentAttempt` by `PaymentTransactionId` before applying verified payment. Neither message adds a
`DeliveryId`; consumer inbox persistence makes routing and application safe under redelivery.

See [[persistence]] for how the outbox table itself is persisted, and [[unit-testing]]/[[integration-testing]] for handler/background-service test coverage requirements.
