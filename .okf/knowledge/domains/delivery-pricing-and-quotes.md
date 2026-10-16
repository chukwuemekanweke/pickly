---
type: domain
title: Delivery Pricing, Routing, Zones and Quotes
description: Configurable vehicle eligibility, PostGIS zones, Mapbox routing, additive pricing and immutable sender-owned quotes
resource: okf://knowledge/domains/delivery-pricing-and-quotes
tags: [constraint, domain, deliveries, pricing, quotes, postgis, mapbox]
code_refs:
    - src/BLVK.Pickly.Logistics.Domain/Deliveries/Entities/VehicleCategory.cs
    - src/BLVK.Pickly.Logistics.Domain/Deliveries/Entities/DeliveryZone.cs
    - src/BLVK.Pickly.Logistics.Domain/Deliveries/Entities/DeliveryPricingPolicy.cs
    - src/BLVK.Pickly.Logistics.Domain/Deliveries/Entities/DeliveryQuote*.cs
    - src/BLVK.Pickly.Logistics.Domain/Deliveries/Services/**
    - src/BLVK.Pickly.Logistics.Application/Deliveries/Quotes/**
    - src/BLVK.Pickly.Logistics.Application/Deliveries/Features/GetVehicleOptions/**
    - src/BLVK.Pickly.Logistics.Application/Deliveries/Features/CreateDeliveryQuote/**
    - src/BLVK.Pickly.Logistics.Application/Deliveries/Features/GetDeliveryQuote/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Deliveries/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Configurations/Delivery*.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Configurations/VehicleCategoryConfiguration.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/DeliveryZoneReadModelRepository.cs
    - src/BLVK.Pickly.Logistics.WebAPI/Features/Deliveries/Quotes/**
    - tests/**/Deliveries/Quotes/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Domain/Deliveries/Services/DeliveryPricingCalculator.cs
    - kind: url
      url: https://docs.mapbox.com/api/navigation/directions/
generated:
    at: "2026-10-16T00:00:00Z"
    by: codex
status: stable
governance: constraint
---
## Vehicle eligibility and recommendation

`VehicleCategory` is persisted configuration, not an enum. Active categories are weight-eligible when their
`MaximumWeightKg` covers `sum(quantity * WeightKg)`; dimensions are not claimed because parcels have no dimension
input. `RoutingProfile` must be an official Mapbox Directions profile (`driving`, `driving-traffic`, `walking`, or
`cycling`); commercial vehicle names do not imply provider-specific road restrictions. Options rank by total, then
capacity, code and id. Quote creation always repeats eligibility, routing and pricing validation.

## Zones and spatial semantics

`DeliveryZone` metadata is domain-owned; Infrastructure maps its shadow `Boundary` to
`geometry(MultiPolygon,4326)` so NetTopologySuite stays outside Domain. The GiST-backed lookup constructs points
longitude-first and uses `ST_Covers`, including boundary points. Active/effective matches order by descending priority.
A unique top priority wins; equal top priorities are rejected as ambiguous. There is no nearest-zone fallback and the
migration seeds no invented boundaries.

## Routing and reuse

`IDeliveryRouteProvider` receives ids and coordinates loaded from the shared Location Registry. The Mapbox client calls
Directions v5 with `overview=false`, stores no geometry or raw response, validates supported profiles and distinguishes
no-route, throttling, timeout, configuration, invalid-response and provider failures. Straight-line PostGIS distance is
never a commercial fallback. Default HTTP request logging is removed because the token is in the query string.

Successful scalar distance/duration results use a configurable five-minute Redis `IJsonCache` entry keyed by provider,
ordered origin/destination ids and profile; an in-process keyed gate coalesces same-instance calls. Direction matters.
Public Directions documentation does not grant Permanent Geocoding-style storage rights, so cache life is short. The
immutable distance on an issued quote is commercial evidence rather than a reusable response cache; production must
confirm that use against its executed Mapbox agreement before launch.

## Pricing and quotes

`DeliveryPricingPolicy` is an immutable effective-dated version using the Payments `CurrencyId`, with optional
delivery/order applicability, base fee, per-kilometre distance rate, per-kilogram weight rate, and required vehicle and
directional zone-pair rules. No production rates are seeded. Explicit zero is valid. Pure decimal calculation rounds
variable components to two decimals away from zero and checked-sums all five components. No complete policy is
pricing-unavailable; multiple complete policies are a configuration error.

The authenticated APIs are `POST /api/v1/deliveries/vehicle-options`, `POST /api/v1/delivery-quotes`, and
`GET /api/v1/delivery-quotes/{quoteId}`. Pickup ids identify sender-owned `SavedPickupLocation` records; destination ids
identify usable canonical Locations. Cross-owner pickup and quote reads use not-found semantics. External routing runs
before the short quote save.

`DeliveryQuote` snapshots addresses, parcels, total weight, vehicle, zones, routed metres, policy version, five price
components, currency and expiry. Retrieval never reroutes, re-zones or reprices. Expiration derives through
`TimeProvider`; no job mutates expired rows. `Consume` is Phase 4-ready only: it requires an unexpired quote, permits an
idempotent retry for the same delivery and rejects another consumer. PostgreSQL `xmin` is mapped as the quote row-version
so competing Phase 4 consumers cannot both save successfully. Phase 4 should consume and create the Delivery in
one transaction, construct the existing `DeliveryCharge`, and add `VehicleCategoryId` to Delivery as the narrow
fulfillment-facing extension; Phase 3 exposes no booking or consumption endpoint.
