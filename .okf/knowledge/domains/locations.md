---
type: domain
title: Shared Location Registry (Locations Domain)
description: Provider-neutral shared Location aggregate on PostGIS, Mapbox Geocoding v6 permanent resolution, search vs resolution separation, conservative deduplication, cross-instance resolution lease, landmark fallback and privacy rules
resource: okf://knowledge/domains/locations
tags:
    - constraint
    - domain
    - locations
    - postgis
    - geocoding
    - mapbox
governance: constraint
code_refs:
    - src/BLVK.Pickly.Logistics.Domain/Locations/**
    - src/BLVK.Pickly.Logistics.Application/Locations/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Locations/**
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Location*.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/GeographicCoordinatesConverter.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Persistence/Configurations/Location*.cs
    - src/BLVK.Pickly.Logistics.Infrastructure/Migrations/*AddSharedLocationRegistry*
    - src/BLVK.Pickly.Logistics.WebAPI/Features/Locations/**
    - tests/unit/BLVK.Pickly.Logistics.Domain.UnitTests/Locations/**
    - tests/unit/BLVK.Pickly.Logistics.Application.UnitTests/Locations/**
    - tests/unit/BLVK.Pickly.Logistics.Infrastructure.UnitTests/Locations/**
    - tests/unit/BLVK.Pickly.Logistics.WebAPI.UnitTests/Features/Locations/**
    - tests/integration/BLVK.Pickly.Logistics.WebAPI.IntegrationTests/Locations/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Domain/Locations/Entities/Location.cs
    - kind: file
      path: src/BLVK.Pickly.Logistics.Application/Locations/Features/ResolveLocation/ResolveLocationHandler.cs
generated:
    at: "2026-10-15T00:00:00Z"
    by: claude
status: stable
---
## Model

`Location` (schema `locations`) is a canonical, reusable physical place shared by every sender. It holds only
canonical data: `LocationAddress` (display/normalized address, line, city, state, ISO country), WGS84
`GeographicCoordinates`, `LocationType` (`Address`, `Landmark`), `ResolutionSource` (`Mapbox`,
`InternallyRegistered`), `ResolutionStatus` (`Resolved`, `Retired`), `Visibility` and `ResolvedAtUtc`. It must never
carry sender data (labels, directions, phone numbers, access codes) — those live on referencing records such as
`SavedPickupLocation` (see [[deliveries]]). A location is usable only while `Resolved` and not soft-deleted.

Children:
- `LocationProviderReference` — provider provenance (provider, feature id, feature type, storage classification
  `PermanentGeocoding`). Unique on `(Provider, FeatureId)`. Provider ids never appear on `Location` itself.
- `LocationResolutionKey` — SHA-256 of a normalized resolution request (`LocationAddress.ComputeResolutionKey`),
  unique. Only hashes are stored so raw customer input is not retained.

Factories: `Location.RegisterProviderResolved` (always `Address` type, explicit provider resolution source and
permanent provenance) and
`Location.RegisterLandmark` (internally curated, always `Public`). There is deliberately no API that accepts
client-supplied coordinates; landmarks are created by internal operations, never from failed geocoding.

## Spatial storage (PostGIS)

- Coordinates map to `geography(Point,4326)` through `GeographicCoordinatesConverter` — the only place that applies
  NetTopologySuite ordering (X = longitude, Y = latitude). Domain has no NetTopologySuite dependency.
- GiST index `IX_Locations_Coordinates`; geography makes `ST_DWithin`/`ST_Distance` work in metres. Never compute
  distances from raw degrees.
- Proximity (`LocationReadModelRepository.FindNearbyAsync`) is explicit SQL: `ST_DWithin` filter + `ST_Distance`
  ordering, bounded by radius and limit, public usable locations only, anchor excluded.
- Text search uses a GIN expression index `to_tsvector('simple', "DisplayAddress")` created in the migration; queries
  must use the identical expression with an inline `'simple'` literal. Terms are sanitized to letters/digits and
  prefix-matched — no leading-wildcard `LIKE`.

## Search vs resolution (strictly separate)

- `SearchLocations`, `GetLocation`, `GetNearbyLocations` read only the registry and never call a provider.
- `ResolveLocation` order: supported-country check → registry lookup (resolution key, then a *unique*
  normalized-address match) → cross-instance lease → recheck registry → cached recent provider outcome → Mapbox.
- Auto-accept only when exactly one registrable candidate has `Exact`/`High` confidence and no other candidate is
  better than `Low`. Registrable = `Premise` or `Street` precision, matching country, valid coordinates and address.
  Otherwise candidates are held server-side (`PendingLocationSelection` in `IJsonCache`, bound to the stakeholder)
  and clients get opaque selection tokens for `SelectLocationCandidate`. A selected candidate is registered without
  recording the request text as its resolution key (another sender may mean a different candidate).
- Unresolved: no coordinates are fabricated. If the provider returned only an `Area` result it is used, unpersisted,
  as an anchor to suggest nearby public `Landmark`s; the sender must explicitly pick one.

## Deduplication and concurrency

Matching priority: LocationId → provider reference → resolution key / unique normalized full address → new location.
Normalization only folds case, whitespace and punctuation — no abbreviation expansion, and a street is never merged
with premises on it. `ILocationResolutionLease` is a database lease (`locations."LocationResolutionLeases"`) taken by
one autocommit upsert that only steals expired leases; waiters poll the registry and give up with
`ResolutionInProgress` (409). The provider call happens outside any transaction; `LocationRegistrationStore` then
uses a short transaction with an advisory lock on the provider feature, and the unique index is the final guard.

## Mapbox

`MapboxGeocodingClient` calls Geocoding v6 structured forward (`search/geocode/v6/forward`, `permanent=true`,
`autocomplete=false`, country filter, bounded `limit`). It refuses (`PermanentStorageUnavailable`, no HTTP call)
unless `Locations:Mapbox:PermanentStorageEnabled` is true and a token is configured — never downgrade to temporary
results, which may not be stored. 5xx/transport errors get bounded Polly retries; 429 is not retried and surfaces a
retry hint; 403 means permanent storage is not permitted. The token travels in the query string, so default
HttpClient loggers are removed and logs/metrics never include tokens or addresses. Search Box results do not carry the
same storage rights and must not be persisted through this path.

## Privacy, limits and prerequisites

- Mapbox premise results are `Private` (reachable only by exact resolution or id); street-level results and landmarks
  are `Public` (searchable and proximity-discoverable). Search/nearby never return private locations.
- Resolution endpoints use the per-user `LocationResolutionPolicy` rate limit; query length, radius, limits and
  supported countries are bounded (`LocationResolutionOptions`).
- Production PostgreSQL must have the PostGIS extension available; the migration runs `CREATE EXTENSION IF NOT
  EXISTS postgis`, which needs sufficient privileges on managed services (pre-create it otherwise).

## Future consumers

Delivery creation copies a usable location's address/coordinates into the immutable `DeliveryAddress` snapshot; it
never references `Location` mutably. Zones, routing, pricing and rider proximity should query the same `geography`
column with GiST-backed PostGIS predicates.
