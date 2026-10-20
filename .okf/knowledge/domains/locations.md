---
type: domain
title: Shared Location Registry (Locations Domain)
description: Canonical street and landmark registry on PostGIS with registry-first search, explicit token-based registration, area-only suggestions and privacy rules
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
`GeographicCoordinates`, `LocationType` (`Street`, `Landmark`, `Area`), `ResolutionSource` (`Mapbox`,
`InternallyRegistered`), `ResolutionStatus` (`Resolved`, `Retired`), `Visibility` and `ResolvedAtUtc`. It must never
carry sender data (labels, directions, phone numbers, access codes) — those live on referencing records such as
`SavedPickupLocation` (see [[deliveries]]). A location is usable only while `Resolved`, not soft-deleted, and typed as
a `Street` or `Landmark`; areas may be discoverable context but are never usable as pickup or delivery points.

Children:
- `LocationProviderReference` — provider provenance (provider, feature id, feature type, storage classification
  `PermanentGeocoding`). Unique on `(Provider, FeatureId)`. Provider ids never appear on `Location` itself.
- `LocationResolutionKey` — SHA-256 of a normalized resolution request (`LocationAddress.ComputeResolutionKey`),
  unique. Only hashes are stored so raw customer input is not retained.

Factories: `Location.RegisterProviderResolved` (provider street or landmark, explicit provider resolution source and
permanent provenance) and
`Location.RegisterLandmark` (internally curated, always `Public`). There is deliberately no API that accepts
client-supplied coordinates; landmarks are created by internal operations, never from failed geocoding.

Curated reference locations (Enugu: post-deploy `0005.enugu-locations.postdeploy.sql`) use the
`RegisterProviderResolved` shape with `ResolutionSource = InternallyRegistered` and an OpenStreetMap provenance
reference (`Provider = 'openstreetmap'`, `FeatureId = way/<id>|node/<id>`, ODbL permits permanent storage with
attribution). The OSM reference is their idempotency key; a seed row is skipped when a usable location with the same
normalized identity already exists, so seeding never duplicates a Mapbox-registered location. Names used in more
than one locality are excluded rather than merged. See [[database-seeding]].

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
  normalizes the query, and searches resolved public registry locations first. Indexed partial street/landmark matches
  are sufficient to suppress Mapbox as soon as one usable registry result exists; full normalized display-address
  equality is not required for discovery. Exact-address identity resolution retains its stricter matching rules.
- When registry results are not reliable, search takes the cross-instance resolution lease, reuses a cached recent
  permanent-geocoding outcome when available, and otherwise calls Mapbox. Search never creates a `Location`.
- Registry and provider matches share one result contract with `LocationType` and `IsSelectable`. Registered streets
  and landmarks contain a `LocationId`; new verified ones contain an opaque selection token. Registered areas may
  retain their real `LocationId` but are explicitly nonselectable; provider area suggestions contain neither an id nor
  a token and are never persisted. Area-only registry matches permit provider fallback. No useful provider result
  returns an empty collection; provider outages remain explicit errors.
- Registrable provider candidates are canonical `Street` or `Landmark` precision, match the supported country, and
  carry a valid canonical address and WGS84 coordinates. A premise is accepted only when Mapbox supplies reliable
  structured parent-street identity; the provider street id and street/locality display become the canonical result,
  so different building numbers reuse one `LocationId`. Candidates are held server-side (`PendingLocationSelection` in
  `IJsonCache`) and bound to the stakeholder; clients never submit provider payloads or coordinates.
- `POST /api/v1/locations` accepts only a selection token. It validates token ownership/expiry, registers or reuses
  the provider feature through `LocationCandidateRegistrar` and `LocationRegistrationStore`, returns `201 Created`
  for a new canonical location and `200 OK` for a reused one. Repeating a still-valid token is idempotent because
  provider-reference locking and uniqueness return the existing location.
- `POST /locations/resolve` and `POST /locations/resolve/selections` are not public routes. The internal resolution
  helpers remain available for orchestration and tests, but pickup-location handlers accept only `LocationId` and
  never geocode.
- Unresolved searches return no fabricated coordinates. A relevant provider area may be shown only as a nonselectable
  suggestion; city/state-only or irrelevant results return nothing. Areas cannot be pickup, quote, or saved locations.

## Deduplication and concurrency

Matching priority: LocationId → provider reference → resolution key / unique normalized full address → new location.
Normalization only folds case, whitespace and punctuation — no abbreviation expansion. Provider street identity plus
country/state/city/locality context prevents same-named streets in different localities from being merged, while a
distinct named landmark never collapses into its parent street. `ILocationResolutionLease` is a database lease (`locations."LocationResolutionLeases"`) taken by
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

- Only canonical streets and distinct landmarks are registered, and both are `Public`. Premises and areas are never
  persisted; private building numbers and access directions remain sender-specific metadata.
- Search and registration use the per-user `LocationResolutionPolicy` rate limit; query length, radius, limits and
  supported countries are bounded (`LocationResolutionOptions`). Location-resolution and Mapbox operational options
  contain no C# policy defaults; both configuration sections are required and validated during service registration.
- Search telemetry records the actual trimmed search term, normalized search term, registry/selectable/provider/result
  counts, lookup outcome, source, cache use, country and resolution origin as structured event properties.
- Production PostgreSQL must have the PostGIS extension available; the migration runs `CREATE EXTENSION IF NOT
  EXISTS postgis`, which needs sufficient privileges on managed services (pre-create it otherwise).

## Delivery pricing consumer

Delivery quote creation loads usable canonical street/landmark locations by id, resolves delivery-zone membership
against their representative coordinates and copies their address/coordinates into immutable quote snapshots; it
never geocodes or modifies a `Location`. The same `LocationId` and coordinate drive Mapbox routing, Redis cache keys,
and PostGIS membership. A long street intentionally has one representative point and one zone; no routing anchor,
street segmentation, or multi-zone membership exists. See
[[delivery-pricing-and-quotes]].
