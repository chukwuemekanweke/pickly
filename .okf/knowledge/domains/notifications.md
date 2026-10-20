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
- Mailtrap sends use the `notifications-mailtrap` named `HttpClient`, passed into the Mailtrap SDK, so standard
  HttpClient OpenTelemetry produces provider request spans with HTTP route/status/exception data. Do not attach
  recipient addresses, message bodies, API tokens or other email content to transport spans.
- Delivery lifecycle notifications reuse `SendNotificationCommand` and the transactional outbox; application and
  consumer handlers only compose template data and never call the email provider directly. Stable message identities
  are derived from the business transition, notification type, and recipient so operation replay does not create a
  distinct notification.
- The delivery set contains localized English/French templates for order creation, actionable payment instructions,
  confirmed payment failure, applied payment success, sender-facing rider assignment, and rider-facing fulfillment
  instructions. Dynamic body replacements are HTML-encoded, sensitive instruction/contact fields are obfuscated in
  notification logs, and expired payment-instruction commands are consumed without sending.
- The default template set also contains matching English/French branded layouts for the ten legacy account,
  authentication, security, trial, subscription and invoice notification types. Email-confirmation OTP,
  password-reset OTP, successful-sign-in and account-lock messages have active Consumer producers; the other legacy
  templates remain seeded compatibility fallbacks until a producer dispatches them. Trial and subscription copy must
  not imply delivery cancellation, retention guarantees or billing capabilities beyond the supplied placeholders.
- Every localized transactional template includes a 440x280 Pikly illustration. Illustration masters are checked in as
  SVG sources alongside email-compatible PNGs. Their manually managed public R2 URLs are explicit
  `NotificationIllustrationUrls` values included in each notification command; neither template rendering nor database
  migration uploads or derives asset URLs. Upload every new PNG to the matching `transactional/v1` object path before
  deploying a producer or template that references it, and include `IllustrationUrl` in any producer added for a dormant
  legacy template.
- Delivery status comes back via the Mailtrap delivery webhook (`ProcessMailtrapDeliveryWebhook` in Application, received at `WebAPI/Features/EmailNotifications/Webhooks`), recorded as an
  `EmailDeliveryWebhookInbox` entry and published as `EmailDeliveryWebhookReceived` (`Contracts.Events`) through the outbox for downstream handling in `Consumer` — see [[async-processing]].

Follows the standard [[vertical-slices]] feature-folder shape and the [[unit-testing]]/[[integration-testing]] conventions.
