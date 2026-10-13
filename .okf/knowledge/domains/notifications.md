---
type: domain
title: Notifications Domain
description: Email notifications sent via the Consumer, Mailtrap delivery webhook processing, and email templates
resource: okf://knowledge/domains/notifications
tags:
    - context
    - domain
    - notifications
    - email
governance: context
code_refs:
    - src/BLVK.Pickly.Logistics.Application/Notifications/**
    - src/BLVK.Pickly.Logistics.Domain/Notifications/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Notifications/**
    - src/BLVK.Pickly.Logistics.WebAPI/Features/EmailNotifications/**
    - src/BLVK.Pickly.Logistics.Consumer/Notifications/**
    - src/BLVK.Pickly.Logistics.Consumer/EmailTemplates/**
    - src/BLVK.Pickly.Logistics.Contracts/Commands/Notifications/**
    - tests/unit/BLVK.Pickly.Logistics.Consumer.UnitTests/Notifications/**
    - tests/integration/BLVK.Pickly.Logistics.WebAPI.IntegrationTests/EmailNotifications/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Domain/Notifications/Entities/EmailNotificationLog.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Scope

- `SendNotificationCommand` (`Contracts`) carries `NotificationContent` for a given `NotificationMedium`/`NotificationType` (email, SMS, app push, web push are modeled as content types even where only
  email is currently wired up end to end).
- The actual send happens in `Consumer` using `EmailTemplates/TemplateSets` (a `default` template set), driven by an `EmailNotificationTemplate`/`EmailNotificationLog` record for tracking.
- Delivery status comes back via the Mailtrap delivery webhook (`ProcessMailtrapDeliveryWebhook` in Application, received at `WebAPI/Features/EmailNotifications/Webhooks`), recorded as an
  `EmailDeliveryWebhookInbox` entry and published as `EmailDeliveryWebhookReceived` (`Contracts.Events`) through the outbox for downstream handling in `Consumer` — see [[async-processing]].

Follows the standard [[vertical-slices]] feature-folder shape and the [[unit-testing]]/[[integration-testing]] conventions.
