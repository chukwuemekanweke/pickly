---
type: infrastructure
title: Transactional Outbox Implementation
description: How OutboxWriter, the outbox table, RabbitMqOutboxMessageDispatcher and the Jobs processor implement at-least-once delivery
resource: okf://knowledge/infrastructure/transactional-outbox
tags:
    - constraint
    - infrastructure
    - outbox
    - messaging
    - rabbitmq
governance: constraint
code_refs:
    - src/BLVK.Pickly.Logistics.Infrastructure/Messaging/**
    - src/BLVK.Pickly.Logistics.Jobs/OutboxProcessing/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Infrastructure/Messaging/OutboxWriter.cs
    - kind: file
      path: src/BLVK.Pickly.Logistics.Jobs/OutboxProcessing/RabbitMqOutboxMessageDispatcherConstants.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Mechanism

1. Application handlers append to the outbox (`IOutboxWriter`) inside the same EF Core transaction as the domain write. This guarantees the "event to publish" is durable if and only if the domain change committed.
2. `Jobs.OutboxProcessing.OutboxMessageProcessor`, woken via `OutboxNotificationListener` (Postgres `LISTEN/NOTIFY`) or polling per `OutboxProcessingOptions`, picks up undispatched rows.
3. `RabbitMqOutboxMessageDispatcher` publishes each row's payload to RabbitMQ and marks it dispatched; failures leave the row pending for retry rather than silently dropping it.
4. `Consumer` handlers (see [[async-processing]]) receive the message and use a message inbox to deduplicate redelivery.

## Constraint

Do not publish to RabbitMQ directly (bypassing the outbox) from an Application/WebAPI request path — this reintroduces the dual-write problem the outbox exists to solve. Any new async side effect
triggered from a request handler goes through `IOutboxWriter`.
