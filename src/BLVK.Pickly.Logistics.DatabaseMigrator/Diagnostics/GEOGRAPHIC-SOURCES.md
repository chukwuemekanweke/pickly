# Enugu reference data: sources, method and review status

Status: **DRAFT, awaiting geographic and commercial approval.** The data is seeded by
`Scripts/PostDeploy/0004`–`0006`, which run on every deployment, including production. Sign off the review items at
the end before the first production deployment: ENU-V1 is immutable once it exists in an environment.

## Source and licence

- **Source:** OpenStreetMap, © OpenStreetMap contributors, licensed under the
  [ODbL 1.0](https://opendatacommons.org/licenses/odbl/). OSM permits permanent storage with attribution, so
  these rows can be kept in the registry. Any user-facing map or location list built from them must show the
  attribution "© OpenStreetMap contributors".
- **Extraction:** via the Overpass API on 2026-10-18; data timestamp `osm_base` 2026-10-18T21:28:20Z.
- **Provenance:** every seeded `Location` has a `LocationProviderReference` row with `Provider = 'openstreetmap'`
  and `FeatureId = way/<id>` or `node/<id>`. To check a record, open
  `https://www.openstreetmap.org/<FeatureId>`.
- **No Mapbox data is stored.** Delivery distance still comes from Mapbox Directions at quote time.

### Overpass queries used

```text
// Urban LGA boundaries (outer ways, assembled with ST_BuildArea)
rel(3717217);way(r:"outer");out geom;     // Enugu South
rel(14306407);way(r:"outer");out geom;    // Enugu North
rel(14306408);way(r:"outer");out geom;    // Enugu East

// Neighbourhood points (zone seeds)
nwr(area.lga)["place"]; out center tags;

// Named streets
way(area.lga)["highway"~"^(trunk|primary|secondary|tertiary|unclassified|residential|living_street)$"]["name"];
out geom tags;

// Landmark candidates
nwr(area.lga)["name"]["amenity"~"^(hospital|university|college|marketplace|bus_station|police|townhall|courthouse|stadium|library|fire_station|post_office)$"];
nwr(area.lga)["name"]["shop"~"^(mall|supermarket|department_store)$"];
nwr(area.lga)["name"]["leisure"~"^(stadium|park)$"];
nwr(area.lga)["name"]["tourism"~"^(museum|hotel|attraction)$"];
nwr(area.lga)["name"]["aeroway"="aerodrome"];
out center tags;
```

Here `area.lga` is the union of the three LGA relations, built with `rel(<id>);map_to_area`.

## Service area and zones

**Service area.** The union of the OSM boundaries of the Enugu South, Enugu North and Enugu East LGAs: 557.1 km²,
a single valid polygon. The LGA boundaries come from an eHealth Africa import (`source:date=2013`).

**Zone method.** Each zone is the Voronoi cell of one OSM neighbourhood point, clipped to the service area. This
gives the following properties, all of which `enugu-validation.sql` re-checks on the live database:

- The 10 zones tile the area exactly: union area = sum of areas = 557.1 km².
- There are no overlaps and no gaps.
- Every polygon is valid, SRID 4326, MultiPolygon, longitude-first.

**Zone boundaries are perpendicular bisectors between neighbourhood centres.** They do not follow surveyed
neighbourhood borders or major roads.

**Priorities.** Each zone gets a distinct priority (101–110). The only purpose is to break ties on shared edges:
`ST_Covers` includes boundaries, and the resolver rejects equal top priorities.

| Code | Name | Seed point (OSM) | Area km² | Streets | Landmarks |
|---|---|---|---:|---:|---:|
| ENU-ABK | Abakpa Nike | node/13153907403 | 143.5 | 13 | 2 |
| ENU-ACH | Achara Layout | node/13170463399 | 28.3 | 13 | 4 |
| ENU-EME | Emene | node/501497579 | 210.7 | 13 | 3 |
| ENU-GRA | GRA | node/13170601816 | 12.3 | 13 | 8 |
| ENU-IND | Independence Layout | node/13153907401 | 28.2 | 13 | 7 |
| ENU-NHV | New Haven | node/13153907405 | 9.5 | 13 | 3 |
| ENU-OGU | Ogui | node/13172378167 | 7.4 | 13 | 6 |
| ENU-TEK | Trans-Ekulu | node/13170601817 | 67.5 | 13 | 4 |
| ENU-THC | Thinkers Corner | node/13153907406 | 9.3 | 13 | 1 |
| ENU-UWA | Uwani | node/13170463400 | 40.3 | 13 | 5 |

### Reviewing the polygons

Export the zones as GeoJSON and open them in [geojson.io](https://geojson.io) or QGIS, over an OSM basemap:

```sql
SELECT json_build_object('type', 'FeatureCollection', 'features', json_agg(json_build_object(
    'type', 'Feature',
    'properties', json_build_object('code', "Code", 'name', "Name", 'priority', "Priority"),
    'geometry', ST_AsGeoJSON("Boundary")::json)))
FROM deliveries."DeliveryZones" WHERE "Code" LIKE 'ENU-%' AND "IsDeleted" = FALSE;
```

To change a boundary, edit the WKT in `Scripts/PostDeploy/0004.enugu-delivery-zones.postdeploy.sql` and deploy. Existing zones have
their Name, Priority and Boundary realigned in place. Then rerun `enugu-validation.sql`, because its
adjacency check compares the tariff against the new shared edges.

## Locations

The seed contains **173 locations: 130 streets (13 per zone) and 43 landmarks.**

### 2026-10-20 reviewed expansion

A fresh six-query Overpass inventory covered the same three LGAs with `osm_base` timestamps from
2026-10-20T13:34:39Z through 2026-10-20T13:39:51Z. It returned 4,537 distinct OSM features: 3,248 named highway
ways, 1,194 named landmarks and 95 named areas. Of those, 4,364 were not present by OSM reference and 4,034 were
not obvious normalized-name matches. These are raw review counts, not safe-to-import counts; segmented streets,
areas, private premises, duplicates, misspellings and unverified names remain in the inventory.

Post-deploy `0008.enugu-locations-expansion.postdeploy.sql` makes 51 reviewed canonical landmark additions and one
provenance-only addition. All new canonical rows use mapped OSM nodes, not way/area centres:

- **Annunciation Hospital** — one new landmark using OSM `node/8753751530` at 6.4684403 N,
  7.5919103 E. OSM spells the tag `Announciation Specialist hospital`; the canonical spelling is supported by the
  hospital's official site and provider directories. The shorter canonical display name keeps the landmark distinct
  from Annunciation Hospital Road without inheriting the OSM spelling error. Because this is an OSM node, the coordinate is the mapped
  hospital point rather than an `out center` approximation of a way.
- **Annunciation Hospital Road** — no new street. OSM `way/241655569` shares node `2482826186` with the already
  seeded `way/241655401`, proving the ways are connected segments of the same named physical street. The new way is
  attached as a second provider reference to the existing canonical Location.
- **Other reviewed nodes** — 8 additional hospitals/clinics, 4 markets, 3 public-service landmarks, 9 educational
  institutions, 9 churches, 9 bus stops, and 8 commercial landmarks (supermarkets, plazas and hotels). Canonical
  spelling/casing is normalized where the OSM tag is clearly malformed. The full provider-reference-to-name mapping
  is explicit in `0008`; no raw candidate file is executed as SQL.

The expansion brings the reviewed seed to **224 canonical locations: 130 streets and 94 landmarks**, with 225 OSM
provider references. It remains insert-only: if a usable Mapbox location already has the normalized identity, the
new OSM reference attaches to that location rather than changing coordinates or creating a duplicate.

`Bisalla Road` remains unresolved. No named OSM highway matched `Bisalla` or `Bisala`; only nearby POI address tags
use `Bisala` (for example `node/2455133925`). Without a verifiable named way and geometry, no street or coordinate
was invented.

**Street selection rules:**
- Same-name OSM ways within ~200 m are clustered into one street.
- Names that form more than one cluster are excluded (82 names). These are the same name used in different
  localities, so excluding them avoids merging different streets.
- Length must be 300 m to 10 km. Intercity expressways are excluded.
- The name must start with a capital letter and end in a street type (Road, Street, Avenue, Close, Crescent,
  Drive, Lane, Way).
- Within each zone, streets are ranked by road class, then length.
- **Representative point:** the midpoint of the street's longest merged segment. `FeatureId` is the OSM way at
  that point.

**Manually excluded street names** (OSM data-quality problems; not corrected, left out):

| Name | Reason |
|---|---|
| Old Abakiliki Road | misspelled variant of Old Abakaliki Road (which is seeded) |
| Abakiliki Road | misspelled variant |
| Enugu Porthacourt Road | misspelling of Port Harcourt |
| Enugu - Port Hacourt Road | misspelling of Port Harcourt |
| Salvage Crescent | conflicts with Savage Crescent (seeded) |
| Udeji Amechi Ukononu, Udeji Amechi Ukunonu | inconsistent spellings, no street type |
| wales road, Penoks street | unverified casing or spelling |
| Fsp Avenue | unverified abbreviation |
| Ugbo-Odogwu | no street type; may be an area |
| Artisan Quarters | area name, not a street |
| Link Road | generic name |
| Robison Street | possible misspelling of Robinson Street |
| Old Garriki Road | spelling conflicts with the Gariki place name |

**Landmarks.** 43 distinct, well-known places were hand-picked from 216 OSM candidates: markets, hospitals,
universities, stadiums, the airport, malls, parks and major hotels. Where OSM had duplicates (a node and a way,
or variant spellings), one record was kept.

**Address shape.** For every location:
- `AddressLine` = the OSM name
- `City` = `Enugu`, `State` = `Enugu`, `CountryCode` = `NG`
- `DisplayAddress` = `<name>, Enugu, Enugu State`

This matches Mapbox context naming, so a later Mapbox registration of the same street normalizes to the same
identity. The seed skips any row whose normalized identity already exists in the registry.

**Way points.** For landmarks that are OSM ways, the point is the Overpass bounding-box centre. For large sites
this is not the gate or terminal. In particular, Akanu Ibiam International Airport resolves to the airfield, not
the terminal.

## ENU-V1 directional zone matrix (origin rows × destination columns, NGN)

Rule used to fill the matrix:

- Same zone: ₦0 (approved)
- Zones sharing an edge: ₦200 (approved)
- Two hops apart: ₦400 (**proposed**)
- Three hops apart: ₦600 (**proposed**)

The matrix is symmetric, but it is stored as 100 directional rows so each direction can be changed on its own.

| from \ to | ABK | ACH | EME | GRA | IND | NHV | OGU | TEK | THC | UWA |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| **ABK** | 0 | 600 | 200 | 400 | 400 | 400 | 600 | 200 | 200 | 600 |
| **ACH** | 600 | 0 | 600 | 200 | 400 | 400 | 200 | 400 | 400 | 200 |
| **EME** | 200 | 600 | 0 | 400 | 200 | 200 | 400 | 400 | 200 | 400 |
| **GRA** | 400 | 200 | 400 | 0 | 400 | 200 | 200 | 200 | 200 | 400 |
| **IND** | 400 | 400 | 200 | 400 | 0 | 200 | 200 | 600 | 400 | 200 |
| **NHV** | 400 | 400 | 200 | 200 | 200 | 0 | 200 | 400 | 200 | 400 |
| **OGU** | 600 | 200 | 400 | 200 | 200 | 200 | 0 | 400 | 400 | 200 |
| **TEK** | 200 | 400 | 400 | 200 | 600 | 400 | 400 | 0 | 200 | 600 |
| **THC** | 200 | 400 | 200 | 200 | 400 | 200 | 400 | 200 | 0 | 600 |
| **UWA** | 600 | 200 | 400 | 400 | 200 | 400 | 200 | 600 | 600 | 0 |

Counts: 10 same-zone, 40 adjacent, 36 two-hop and 14 three-hop rules.

To change a proposed charge before ENU-V1 is created anywhere, edit the row in
`Scripts/PostDeploy/0006.enu-v1-pricing-policy.postdeploy.sql`. Once ENU-V1 exists in an environment, it is immutable: publish ENU-V2 with a new
effective date instead.

## Open review items (not resolved by this phase)

1. **Zone boundaries need operational review.** ABK and EME include the rural north of Enugu East LGA, which
   makes them very large. Also consider whether boundaries should follow major roads such as Abakaliki Road and
   Ogui Road.
2. **Non-adjacent zone charges need pair-by-pair approval.** The ₦400/₦600 split by hop count is a proposal.
3. **Adjacency is an artefact of the draft polygons.** For example, EME borders IND. Revisit it once the
   boundaries are final.
4. **Ground truth.** Street points and landmark names come from OSM and have not been checked on the ground.
   Validate them before production, especially point placement for large sites such as the airport and UNEC.
5. **Coverage gaps.** Areas outside the three LGAs (for example Ngwo in Udi LGA) are not covered. Quotes there
   return `unsupported_service_area`.
