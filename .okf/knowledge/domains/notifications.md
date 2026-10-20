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
    - src/BLVK.Pickly.Logistics.Application/Deliveries/Features/CreateDelivery/**
    - src/BLVK.Pickly.Logistics.Application/Deliveries/Features/InitiateDeliveryPayment/**
    - src/BLVK.Pickly.Logistics.Application/Payments/Features/ReconcilePayments/**
    - src/BLVK.Pickly.Logistics.Application/Riders/Features/AcceptRiderOffer/**
    - src/BLVK.Pickly.Logistics.Domain/Notifications/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Notifications/**
    - src/BLVK.Pickly.Logistics.WebAPI/Features/EmailNotifications/**
    - src/BLVK.Pickly.Logistics.Consumer/Notifications/**
    - src/BLVK.Pickly.Logistics.Consumer/Payments/ConfirmDeliveryPaymentHandler.cs
    - src/BLVK.Pickly.Logistics.Consumer/EmailTemplates/**
    - src/BLVK.Pickly.Logistics.Contracts/Commands/Notifications/**
    - src/BLVK.Pickly.Logistics.DatabaseMigrator/EmailAssets/**
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
- Delivery lifecycle notifications reuse `SendNotificationCommand` and the transactional outbox; application and
  consumer handlers only compose template data and never call the email provider directly. Stable message identities
  are derived from the business transition, notification type, and recipient so operation replay does not create a
  distinct notification.
- The delivery set contains localized English/French templates for order creation, actionable payment instructions,
  confirmed payment failure, applied payment success, sender-facing rider assignment, and rider-facing fulfillment
  instructions. Dynamic body replacements are HTML-encoded, sensitive instruction/contact fields are obfuscated in
  notification logs, and expired payment-instruction commands are consumed without sending.
- Transactional illustrations are checked in as SVG sources and email-compatible PNGs. Their manually managed public
  R2 URLs are explicit `NotificationIllustrationUrls` values included in each notification command; neither template
  rendering nor database migration uploads or derives asset URLs.
- Delivery status comes back via the Mailtrap delivery webhook (`ProcessMailtrapDeliveryWebhook` in Application, received at `WebAPI/Features/EmailNotifications/Webhooks`), recorded as an
  `EmailDeliveryWebhookInbox` entry and published as `EmailDeliveryWebhookReceived` (`Contracts.Events`) through the outbox for downstream handling in `Consumer` — see [[async-processing]].

Follows the standard [[vertical-slices]] feature-folder shape and the [[unit-testing]]/[[integration-testing]] conventions.
