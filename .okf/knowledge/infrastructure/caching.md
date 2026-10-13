---
type: infrastructure
title: Caching Conventions (Redis-backed JSON cache)
description: IJsonCache abstraction and its Redis-backed DistributedJsonCache implementation
resource: okf://knowledge/infrastructure/caching
tags:
    - constraint
    - infrastructure
    - caching
    - redis
governance: constraint
code_refs:
    - src/BLVK.Pickly.Logistics.Infrastructure/Caching/**
    - src/BLVK.Pickly.Logistics.Domain/Common/Caching/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Infrastructure/Caching/DistributedJsonCache.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Shape

Application code depends on `IJsonCache` (defined in `Domain.Common.Caching`), never on `IDistributedCache`/Redis types directly. `Infrastructure.Caching.DistributedJsonCache` is the only
implementation and serializes values as JSON over the configured distributed cache (Redis in every environment — see [[dev-environment]] for local vs cloud Redis).

## Constraint

New caching needs go through `IJsonCache`. Do not inject `IDistributedCache` or a Redis client into Application/WebAPI code — that keeps caching swappable and keeps cache-key/serialization conventions in one place.
