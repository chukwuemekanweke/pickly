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

Routing also uses the narrow `IDistributedLease` abstraction implemented by `RedisDistributedLease`. Acquisition is
`SET NX` with a bounded expiry and release is an owner-token Lua compare/delete, so abandoned owners expire and cannot
release another owner's lease. Waiting route requests recheck `IJsonCache`; Redis failures degrade to per-instance
coalescing and a direct provider call rather than poisoning the cache.
