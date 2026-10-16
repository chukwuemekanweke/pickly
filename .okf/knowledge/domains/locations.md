---
type: domain
title: Shared Location Registry (Locations Domain)
description: Provider-neutral shared Location aggregate on PostGIS, unified registry-first search with Mapbox fallback, explicit token-based registration, conservative deduplication, cross-instance resolution lease, landmark fallback and privacy rules
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

## Unified search and explicit registration

- `GET /api/v1/locations/search` is the only public resolution entry point. It authenticates the sender, bounds and
  normalizes the query, and searches usable public registry locations first. A full normalized display-address match
  is reliable enough to suppress Mapbox; partial street-name matches remain in the result but do not suppress useful
  provider candidates.
- When registry results are not reliable, search takes the cross-instance resolution lease, reuses a cached recent
  permanent-geocoding outcome when available, and otherwise calls Mapbox. Search never creates a `Location`.
- Registry and provider matches share one result contract. Registry items contain a `LocationId` and no token;
  provider items contain an opaque selection token and no `LocationId`. Provider precision remains visible so a
  street-centre result is not represented as a verified premise or building entrance.
- Registrable provider candidates are `Premise` or `Street` precision, match the supported country, and carry a valid
  canonical address and WGS84 coordinates. Candidates are held server-side (`PendingLocationSelection` in
  `IJsonCache`) and bound to the stakeholder; clients never submit provider payloads or coordinates.
- `POST /api/v1/locations` accepts only a selection token. It validates token ownership/expiry, registers or reuses
  the provider feature through `LocationCandidateRegistrar` and `LocationRegistrationStore`, returns `201 Created`
  for a new canonical location and `200 OK` for a reused one. Repeating a still-valid token is idempotent because
  provider-reference locking and uniqueness return the existing location.
- `POST /locations/resolve` and `POST /locations/resolve/selections` are not public routes. The internal resolution
  helpers remain available for orchestration and tests, but pickup-location handlers accept only `LocationId` and
  never geocode.
- Unresolved searches return no fabricated coordinates. Existing weak registry alternatives can still be selected,
  and registered landmark alternatives remain available through registry search and the nearby-location endpoint;
  the sender must explicitly choose one.

## Deduplication and concurrency

Matching priority: LocationId → provider reference → resolution key / unique normalized full address → new location.
Normalization only folds case, whitespace and punctuation — no abbreviation expansion, and a street is never merged
with premises on it. `ILocationResolutionLease` is a database lease (`locations."LocationResolutionLeases"`) taken by
one autocommit upsert that only steals expired leases; waiters poll the registry and give up with
`ResolutionInProgress` (409). The provider call happens outside any transaction; `LocationRegistrationStore` then
uses a short transaction with an advisory lock on the provider feature, and the unique index is the final guard.

## Mapbox

`MapboxGeocodingClient` calls Geocoding v6 forward (`search/geocode/v6/forward`) with structured fields for internal
address requests and sanitized free text (`q`) for unified search. Every request uses `permanent=true`,
`autocomplete=false`, a supported-country hard filter and a bounded `limit`, because provider candidates and recent
outcomes are held for later canonical registration. The client does not offer a temporary-storage mode. Mapbox is the
authority on whether the configured account may make permanent requests; rejected requests surface as provider
failures. 5xx/transport errors get bounded Polly retries; 429 is not retried and surfaces a retry hint. The token
travels in the query string, so default HttpClient loggers are removed and logs never include tokens or addresses.
Search Box results do not carry the same storage rights and must not be persisted through this path.

## Privacy, limits and prerequisites

- Mapbox premise results are `Private` (reachable only by exact resolution or id); street-level results and landmarks
  are `Public` (searchable and proximity-discoverable). Search/nearby never return private locations.
- Search and registration use the per-user `LocationResolutionPolicy` rate limit; query length, radius, limits and
  supported countries are bounded (`LocationResolutionOptions`).
- Production PostgreSQL must have the PostGIS extension available; the migration runs `CREATE EXTENSION IF NOT
  EXISTS postgis`, which needs sufficient privileges on managed services (pre-create it otherwise).

## Delivery pricing consumer

Delivery quote creation loads usable canonical locations by id, resolves delivery-zone membership against their
existing coordinates and copies their address/coordinates into immutable quote snapshots; it never geocodes or
modifies a `Location`. Delivery creation will later copy the already-agreed quote snapshot. Zone matching uses the
same WGS84 coordinates, while routing sends those server-resolved coordinates to the provider. See
[[delivery-pricing-and-quotes]].
