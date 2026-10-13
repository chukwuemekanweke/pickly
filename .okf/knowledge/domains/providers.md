---
type: domain
title: Providers Domain (Generic Provider Registry)
description: Generic external-service provider registry (e.g. email, file storage) with activation/deactivation, distinct from payment providers
resource: okf://knowledge/domains/providers
tags:
    - context
    - domain
    - providers
governance: context
code_refs:
    - src/BLVK.Pickly.Logistics.Application/Providers/**
    - src/BLVK.Pickly.Logistics.Domain/Providers/**
    - src/BLVK.Pickly.Logistics.WebAPI/Features/Providers/**
    - tests/unit/BLVK.Pickly.Logistics.Application.UnitTests/Providers/**
    - tests/integration/BLVK.Pickly.Logistics.WebAPI.IntegrationTests/Providers/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Domain/Providers/Entities/Provider.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Scope

`Provider` is a generic aggregate keyed by `ProviderType` (currently `Email`, `FileStorage`) with a name/key and an `IsActive` flag, activated/deactivated via the `ActivateProvider` feature. This is
the registry for which concrete external-service implementation is currently active for a given provider type — it is **not** the same concept as a payment provider (Credo/SafeHaven), which is modeled
separately under [[payments]] (`PaymentProvider`, `ActivatePaymentProvider`).

When adding a new externally-swappable integration that needs an on/off or which-implementation-is-active switch, check whether it fits this generic `Provider` registry before inventing a parallel activation mechanism.

Follows the standard [[vertical-slices]] feature-folder shape and the [[unit-testing]]/[[integration-testing]] conventions.
