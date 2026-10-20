/*
    Reviewed Enugu location expansion (DRAFT - pending geographic review).

    Source: OpenStreetMap (c) OpenStreetMap contributors, ODbL 1.0. The inventory was extracted through
    Overpass on 2026-10-20 with osm_base timestamps from 2026-10-20T13:34:39Z through
    2026-10-20T13:39:51Z. See Diagnostics/GEOGRAPHIC-SOURCES.md for review details and unresolved names.

    This expansion deliberately uses reviewed OSM nodes rather than bulk-importing every extracted feature:
      - node/8753751530 is the OSM hospital point for Annunciation Hospital. The OSM name is misspelled
        "Announciation" and includes "Specialist"; the canonical name is corroborated by the hospital's own
        website and kept distinct from the road.
      - additional node rows cover public and delivery-useful hospitals, clinics, markets, schools, universities,
        churches, public services, bus stops, supermarkets and hotels. Nodes are used so coordinates are mapped
        points rather than unsafe Overpass way/area centres. Broad areas and uncertain/private premises are excluded.
      - way/241655569 is a second connected segment of the already-seeded Annunciation Hospital Road.
        It shares node/2482826186 with way/241655401 and is attached to that canonical street rather than
        creating a duplicate Location.

    The script is insert-only and rerun-safe. If Mapbox already registered the same normalized identity,
    the OSM reference is attached to that usable canonical Location. Existing Locations are never changed.
*/

SELECT pg_advisory_xact_lock(hashtext('pickly.seed.enugu-locations-expansion'));

CREATE TEMP TABLE enugu_location_expansion ON COMMIT DROP AS
SELECT
    v.feature_id,
    v.feature_type,
    v.location_type,
    v.name,
    v.latitude,
    v.longitude,
    v.create_location,
    v.name || ', Enugu, Enugu State' AS display_address,
    concat_ws('|',
        lower(regexp_replace(btrim(regexp_replace(v.name, '[^[:alnum:][:space:]]+', ' ', 'g')), '\s+', ' ', 'g')),
        'enugu',
        'enugu',
        'ng') AS normalized_address
FROM (VALUES
    ('node/8753751530',  'poi',    'Landmark', 'Annunciation Hospital',                      6.4684403, 7.5919103, TRUE),
    ('node/8753791521',  'poi',    'Landmark', 'Federal Neuropsychiatric Hospital',          6.4605893, 7.5323680, TRUE),
    ('node/8753212073',  'poi',    'Landmark', 'Mount Carmel Hospital and Mortuary',         6.4585466, 7.5217282, TRUE),
    ('node/8753689869',  'poi',    'Landmark', 'POSH Specialist Hospital',                   6.4582082, 7.5177080, TRUE),
    ('node/8753662599',  'poi',    'Landmark', 'Royal Hospital',                             6.4432724, 7.4821383, TRUE),
    ('node/8753716469',  'poi',    'Landmark', 'The Good Shepherd Hospital',                 6.4269815, 7.4898406, TRUE),
    ('node/2455133013',  'poi',    'Landmark', 'Carolina Specialist Clinic',                 6.4823121, 7.4920407, TRUE),
    ('node/2492311896',  'poi',    'Landmark', 'GTC Clinic Enugu',                           6.4593150, 7.4979710, TRUE),
    ('node/2455133606',  'poi',    'Landmark', 'Hansa Clinics',                              6.4304250, 7.4963290, TRUE),
    ('node/10922065147', 'poi',    'Landmark', 'Akpouga Nike Market',                        6.4681336, 7.6023965, TRUE),
    ('node/13170584597', 'poi',    'Landmark', 'New Artisan Market',                         6.4526825, 7.5417218, TRUE),
    ('node/8753757385',  'poi',    'Landmark', 'Oye Emene Market',                           6.4787045, 7.5784307, TRUE),
    ('node/8753756336',  'poi',    'Landmark', 'Abakpa Timber Market',                       6.4957015, 7.5024551, TRUE),
    ('node/8754484790',  'poi',    'Landmark', 'Enugu Fire Service, Ogui Road',              6.4583145, 7.5098206, TRUE),
    ('node/11396426694', 'poi',    'Landmark', 'Ogui Police Station',                        6.4417471, 7.4952698, TRUE),
    ('node/4440476590',  'poi',    'Landmark', 'Maryland Police Post',                       6.4174437, 7.5096079, TRUE),
    ('node/2455133575',  'poi',    'Landmark', 'Holy Rosary College, Uwani',                 6.4294247, 7.4953809, TRUE),
    ('node/8753756337',  'poi',    'Landmark', 'Coal City University',                       6.4617555, 7.5866013, TRUE),
    ('node/8753753630',  'poi',    'Landmark', 'Godfrey Okoye University',                   6.4874050, 7.5911694, TRUE),
    ('node/8753733505',  'poi',    'Landmark', 'National Open University of Nigeria',        6.4888927, 7.4994790, TRUE),
    ('node/8753679088',  'poi',    'Landmark', 'Queen''s School',                            6.4340241, 7.5065893, TRUE),
    ('node/2455133543',  'poi',    'Landmark', 'Government Secondary School for the Deaf',   6.4243890, 7.4764170, TRUE),
    ('node/8754466854',  'poi',    'Landmark', 'IMT Campus 3',                               6.4289032, 7.5174478, TRUE),
    ('node/6685600511',  'poi',    'Landmark', 'Independence Layout Secondary School',       6.4329787, 7.5224747, TRUE),
    ('node/8754476425',  'poi',    'Landmark', 'Maryland Secondary School',                  6.4141038, 7.5128511, TRUE),
    ('node/8754481308',  'poi',    'Landmark', 'All Saints Church, GRA',                     6.4647120, 7.5256021, TRUE),
    ('node/8573595162',  'poi',    'Landmark', 'Central Methodist Church',                   6.4367705, 7.4949445, TRUE),
    ('node/8754414724',  'poi',    'Landmark', 'Christ the King Catholic Church',            6.4644852, 7.4937725, TRUE),
    ('node/8754447270',  'poi',    'Landmark', 'Maria Assumpta Parish',                      6.4057871, 7.5006292, TRUE),
    ('node/8754480806',  'poi',    'Landmark', 'Maria Rosa Mystica Parish',                  6.4268268, 7.5316665, TRUE),
    ('node/8753791522',  'poi',    'Landmark', 'St. Leo the Great Catholic Church',          6.4799612, 7.5092560, TRUE),
    ('node/8754466855',  'poi',    'Landmark', 'St. Vincent de Paul Catholic Church',        6.4123612, 7.4947242, TRUE),
    ('node/8753728800',  'poi',    'Landmark', 'St. Andrew''s Anglican Church',              6.4832866, 7.4881720, TRUE),
    ('node/8753739943',  'poi',    'Landmark', 'St. Mary''s Catholic Church',                6.4814424, 7.4955850, TRUE),
    ('node/8753690626',  'poi',    'Landmark', 'ESBS Bus Stop',                              6.4320819, 7.5173282, TRUE),
    ('node/8753639566',  'poi',    'Landmark', 'GUO Transport Terminal Bus Stop',            6.4380494, 7.4886302, TRUE),
    ('node/8753705277',  'poi',    'Landmark', 'New Society Bus Stop, Emene',                6.4734815, 7.5824183, TRUE),
    ('node/8753585116',  'poi',    'Landmark', 'Obiagu Bus Stop',                            6.4357490, 7.4972967, TRUE),
    ('node/8753596575',  'poi',    'Landmark', 'Ogbete Bus Stop',                            6.4313654, 7.4767021, TRUE),
    ('node/8752872914',  'poi',    'Landmark', 'Ogui Junction Bus Stop',                     6.4614683, 7.5105516, TRUE),
    ('node/8753608935',  'poi',    'Landmark', 'P&T Bus Stop',                               6.4415410, 7.4873523, TRUE),
    ('node/8753583046',  'poi',    'Landmark', 'Trans-Ekulu Bus Stop',                       6.4782876, 7.4940416, TRUE),
    ('node/8753650489',  'poi',    'Landmark', 'UBA Bus Stop',                               6.4291686, 7.4878720, TRUE),
    ('node/2492315499',  'poi',    'Landmark', 'SPAR, NTA Road',                             6.4367760, 7.5135940, TRUE),
    ('node/11396393143', 'poi',    'Landmark', 'Coal City Plaza',                            6.4358114, 7.4919994, TRUE),
    ('node/8753070715',  'poi',    'Landmark', 'Frank Supermarket',                          6.4581986, 7.5167962, TRUE),
    ('node/13696036519', 'poi',    'Landmark', 'BON Hotel Platinum',                         6.4388958, 7.5149651, TRUE),
    ('node/8753689870',  'poi',    'Landmark', 'Best Western Hotel',                         6.4513950, 7.5304526, TRUE),
    ('node/8753672111',  'poi',    'Landmark', 'De Angelo Hotel',                            6.4588103, 7.4778151, TRUE),
    ('node/2455133808',  'poi',    'Landmark', 'Roban Hotel',                                6.4442174, 7.5101743, TRUE),
    ('node/2455133781',  'poi',    'Landmark', 'Toscana Hotel',                              6.4425750, 7.5237730, TRUE),
    ('way/241655569',    'street', 'Street',   'Annunciation Hospital Road',                 NULL,      NULL,      FALSE)
) AS v(feature_id, feature_type, location_type, name, latitude, longitude, create_location);

DO $$
DECLARE
    problems text;
BEGIN
    SELECT string_agg(issue, '; ') INTO problems
    FROM (
        SELECT 'duplicate OSM reference ' || feature_id AS issue
        FROM enugu_location_expansion GROUP BY feature_id HAVING count(*) > 1
        UNION ALL
        SELECT 'coordinates outside Enugu (check latitude/longitude order) for ' || feature_id
        FROM enugu_location_expansion
        WHERE create_location AND (latitude NOT BETWEEN 6.30 AND 6.70 OR longitude NOT BETWEEN 7.40 AND 7.70)
        UNION ALL
        SELECT 'more than 7 decimal places for ' || feature_id
        FROM enugu_location_expansion
        WHERE create_location AND (round(latitude, 7) <> latitude OR round(longitude, 7) <> longitude)
        UNION ALL
        SELECT 'ambiguous canonical identity ' || e.normalized_address
        FROM enugu_location_expansion e
        JOIN locations."Locations" l
          ON l."NormalizedAddress" = e.normalized_address AND l."IsDeleted" = FALSE
        GROUP BY e.normalized_address
        HAVING count(DISTINCT l."Id") > 1
    ) issues;

    IF problems IS NOT NULL THEN
        RAISE EXCEPTION 'Enugu location expansion rejected: %', problems;
    END IF;
END $$;
INSERT INTO locations."Locations" ("Id", "DisplayAddress", "NormalizedAddress", "AddressLine", "City", "State", "CountryCode", "Coordinates", "LocationType", "ResolutionSource", "ResolutionStatus", "Visibility", "ResolvedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "IsDeleted")
SELECT
    gen_random_uuid(), e.display_address, e.normalized_address, e.name, 'Enugu', 'Enugu', 'NG',
    ST_SetSRID(ST_MakePoint(e.longitude, e.latitude), 4326)::geography,
    e.location_type, 'InternallyRegistered', 'Resolved', 'Public',
    TIMESTAMPTZ '2026-10-20 13:39:51+00', NOW(), 'system', NOW(), FALSE
FROM enugu_location_expansion e
WHERE e.create_location
  AND NOT EXISTS (
      SELECT 1 FROM locations."LocationProviderReferences" r
      WHERE r."Provider" = 'openstreetmap' AND r."FeatureId" = e.feature_id)
  AND NOT EXISTS (
      SELECT 1 FROM locations."Locations" l
      WHERE l."NormalizedAddress" = e.normalized_address AND l."IsDeleted" = FALSE);

INSERT INTO locations."LocationProviderReferences" ("Id", "LocationId", "Provider", "FeatureId", "FeatureType", "StorageClassification", "ResolvedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "IsDeleted")
SELECT
    gen_random_uuid(), l."Id", 'openstreetmap', e.feature_id, e.feature_type, 'PermanentGeocoding',
    TIMESTAMPTZ '2026-10-20 13:39:51+00', NOW(), 'system', NOW(), FALSE
FROM enugu_location_expansion e
JOIN locations."Locations" l
  ON l."NormalizedAddress" = e.normalized_address
 AND l."LocationType" = e.location_type
 AND l."ResolutionStatus" = 'Resolved'
 AND l."Visibility" = 'Public'
 AND l."IsDeleted" = FALSE
WHERE NOT EXISTS (
    SELECT 1 FROM locations."LocationProviderReferences" r
    WHERE r."Provider" = 'openstreetmap' AND r."FeatureId" = e.feature_id);

DO $$
DECLARE
    problems text;
BEGIN
    SELECT string_agg(e.feature_id || ' -> ' || e.normalized_address, '; ') INTO problems
    FROM enugu_location_expansion e
    WHERE NOT EXISTS (
        SELECT 1
        FROM locations."LocationProviderReferences" r
        JOIN locations."Locations" l ON l."Id" = r."LocationId"
        WHERE r."Provider" = 'openstreetmap'
          AND r."FeatureId" = e.feature_id
          AND r."IsDeleted" = FALSE
          AND l."NormalizedAddress" = e.normalized_address
          AND l."LocationType" = e.location_type
          AND l."ResolutionStatus" = 'Resolved'
          AND l."Visibility" = 'Public'
          AND l."IsDeleted" = FALSE);

    IF problems IS NOT NULL THEN
        RAISE EXCEPTION 'Enugu location expansion could not resolve canonical targets: %', problems;
    END IF;
END $$;
