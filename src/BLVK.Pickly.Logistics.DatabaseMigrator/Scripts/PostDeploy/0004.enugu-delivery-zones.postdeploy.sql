/*
    Enugu delivery zones (DRAFT - pending geographic/operational approval).

    Ten zones tile the Enugu urban service area: the union of the OpenStreetMap administrative boundaries of
    the Enugu South (relation/3717217), Enugu North (relation/14306407) and Enugu East (relation/14306408)
    LGAs; see Diagnostics/GEOGRAPHIC-SOURCES.md. Each zone is
    the Voronoi cell of its OSM neighbourhood point clipped to that union, so zones share edges but never
    overlap and leave no gaps. Priorities are unique only to break ties on shared edges (the resolver uses
    ST_Covers, which includes boundaries, and rejects equal top priorities).

    Zone seed points (OSM node, lat/lon):
      ENU-ABK Abakpa            node/13153907403  6.4857321, 7.5261935
      ENU-ACH Achara            node/13170463399  6.4176005, 7.4803533
      ENU-EME Emene             node/501497579    6.4753603, 7.5850857
      ENU-GRA GRA               node/13170601816  6.4601943, 7.4943674
      ENU-IND Independence Layout node/13153907401 6.4429710, 7.5196973
      ENU-NHV New Haven         node/13153907405  6.4581683, 7.5240859
      ENU-OGU Ogui              node/13172378167  6.4479829, 7.5017604
      ENU-TEK Trans-Ekulu       node/13170601817  6.4792234, 7.4883907
      ENU-THC Thinkers Corner   node/13153907406  6.4712385, 7.5272037
      ENU-UWA Uwani             node/13170463400  6.4094433, 7.5011149

    Runs on every deployment (the migrator keeps no execution journal), as one batch and therefore one
    transaction: invalid input raises before anything is written and the whole file rolls back.

    Idempotent: zones are keyed by (CountryCode, Code). Missing zones are inserted active; existing zones
    have Name, Priority and Boundary realigned to this file. IsActive and effective dates are never changed
    after creation, so an operational deactivation is not undone by a rerun. Zones are never deleted here.
*/

SELECT pg_advisory_xact_lock(hashtext('pickly.seed.enugu-delivery-zones'));

CREATE TEMP TABLE enugu_zone_seed ON COMMIT DROP AS
SELECT v.code, v.name, v.priority, ST_GeomFromText(v.wkt, 4326) AS boundary
FROM (VALUES
    ('ENU-ABK', 'Abakpa Nike', 101, 'MULTIPOLYGON(((7.5556318 6.4805019,7.5082015 6.4771961,7.4825955 6.6259169,7.4828086 6.6260448,7.4959049 6.6269178,7.5133667 6.6273541,7.5282092 6.6277909,7.5408688 6.628664,7.5438066 6.6310673,7.545671 6.6325927,7.5482898 6.636085,7.5487266 6.6413236,7.5574574 6.6500545,7.5723 6.6539836,7.5849595 6.6513643,7.5856598 6.6510041,7.5556318 6.4805019)))'),
    ('ENU-ACH', 'Achara Layout', 102, 'MULTIPOLYGON(((7.4780381 6.4419646,7.4967337 6.4287919,7.4773901 6.379559,7.4753876 6.3798351,7.4688883 6.382661,7.4653473 6.3842006,7.4601088 6.3916216,7.4474487 6.4034085,7.4408655 6.4103575,7.4448295 6.4143219,7.4523978 6.4233971,7.4491949 6.4339662,7.4474487 6.4505548,7.447913 6.4518763,7.4780381 6.4419646)))'),
    ('ENU-EME', 'Emene', 103, 'MULTIPOLYGON(((7.5624511 6.4388571,7.5572944 6.4571536,7.5556318 6.4805019,7.5856598 6.6510041,7.6002383 6.6435065,7.6273041 6.618187,7.643456 6.6090198,7.6534967 6.6015983,7.6508775 6.5911212,7.6491313 6.5823903,7.6500044 6.5754061,7.6517506 6.5658021,7.6521869 6.5527058,7.6519638 6.5515901,7.6506657 6.5450989,7.6500044 6.5417919,7.6513138 6.5334978,7.6552429 6.5234575,7.6574254 6.5129805,7.6565523 6.5038128,7.6517506 6.4863515,7.6443291 6.4710722,7.6321058 6.4571028,7.6306801 6.4481907,7.6255579 6.4335299,7.6211925 6.4287276,7.6187498 6.4207562,7.6024213 6.4082103,7.5906343 6.4012256,7.5849595 6.3977332,7.5830569 6.3972575,7.5624511 6.4388571)))'),
    ('ENU-GRA', 'GRA', 104, 'MULTIPOLYGON(((7.5093451 6.4609184,7.4780381 6.4419646,7.447913 6.4518763,7.4495642 6.4565756,7.5077168 6.4748403,7.5098598 6.4684688,7.5093451 6.4609184)))'),
    ('ENU-IND', 'Independence Layout', 105, 'MULTIPOLYGON(((7.5060233 6.4286363,7.5128791 6.4531722,7.5624511 6.4388571,7.5830569 6.3972575,7.5814672 6.3968601,7.5749192 6.3938046,7.5724431 6.3918237,7.5060233 6.4286363)))'),
    ('ENU-NHV', 'New Haven', 106, 'MULTIPOLYGON(((7.5098598 6.4684688,7.5572944 6.4571536,7.5624511 6.4388571,7.5128791 6.4531722,7.5093451 6.4609184,7.5098598 6.4684688)))'),
    ('ENU-OGU', 'Ogui', 107, 'MULTIPOLYGON(((7.5128791 6.4531722,7.5060233 6.4286363,7.4967337 6.4287919,7.4780381 6.4419646,7.5093451 6.4609184,7.5128791 6.4531722)))'),
    ('ENU-TEK', 'Trans-Ekulu', 108, 'MULTIPOLYGON(((7.5082015 6.4771961,7.5077168 6.4748403,7.4495642 6.4565756,7.453124 6.4667068,7.4574895 6.4776206,7.4601355 6.4809284,7.4609818 6.5025034,7.4617158 6.5325959,7.4618549 6.5382996,7.4631643 6.5644922,7.462728 6.5815172,7.4605451 6.6046543,7.4605451 6.6120753,7.4592357 6.6168771,7.4718952 6.6194963,7.4825955 6.6259169,7.5082015 6.4771961)))'),
    ('ENU-THC', 'Thinkers Corner', 109, 'MULTIPOLYGON(((7.5082015 6.4771961,7.5556318 6.4805019,7.5572944 6.4571536,7.5098598 6.4684688,7.5077168 6.4748403,7.5082015 6.4771961)))'),
    ('ENU-UWA', 'Uwani', 110, 'MULTIPOLYGON(((7.4967337 6.4287919,7.5060233 6.4286363,7.5724431 6.3918237,7.5665791 6.3871325,7.5661883 6.3868198,7.5592036 6.3763428,7.545671 6.3702312,7.5295186 6.3719773,7.5114107 6.3764044,7.5104534 6.3766384,7.5103319 6.3766673,7.5103069 6.3766734,7.5098743 6.3767791,7.5011435 6.3750329,7.4995916 6.375395,7.4929219 6.3769514,7.4880471 6.378089,7.4866996 6.3782756,7.4773901 6.379559,7.4967337 6.4287919)))')
) AS v(code, name, priority, wkt);

/* Refuse to write anything unless every polygon is a valid lon/lat MultiPolygon inside Enugu and no two zones
   share interior area. A lat/lon swap would place the polygons near (7.5 N, 6.5 E) and fail the envelope test. */
DO $$
DECLARE
    problems text;
BEGIN
    SELECT string_agg(code || ': ' || issue, '; ' ORDER BY code) INTO problems
    FROM (
        SELECT code, 'invalid geometry (' || ST_IsValidReason(boundary) || ')' AS issue FROM enugu_zone_seed WHERE NOT ST_IsValid(boundary)
        UNION ALL
        SELECT code, 'not a MultiPolygon' FROM enugu_zone_seed WHERE GeometryType(boundary) <> 'MULTIPOLYGON'
        UNION ALL
        SELECT code, 'SRID is not 4326' FROM enugu_zone_seed WHERE ST_SRID(boundary) <> 4326
        UNION ALL
        SELECT code, 'outside the Enugu envelope (check longitude/latitude order)' FROM enugu_zone_seed
        WHERE NOT ST_CoveredBy(boundary, ST_MakeEnvelope(7.40, 6.30, 7.70, 6.70, 4326))
        UNION ALL
        SELECT a.code, 'overlaps ' || b.code FROM enugu_zone_seed a JOIN enugu_zone_seed b ON a.code < b.code
        WHERE ST_Area(ST_Intersection(a.boundary, b.boundary)) > 0
        UNION ALL
        SELECT min(code), 'shares priority ' || priority FROM enugu_zone_seed GROUP BY priority HAVING count(*) > 1
    ) issues;

    IF problems IS NOT NULL THEN
        RAISE EXCEPTION 'Enugu delivery zone seed rejected: %', problems;
    END IF;
END $$;

INSERT INTO deliveries."DeliveryZones" ("Id", "Code", "Name", "CountryCode", "Priority", "EffectiveFromUtc", "EffectiveToUtc", "IsActive", "Boundary", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "IsDeleted")
SELECT gen_random_uuid(), s.code, s.name, 'NG', s.priority, TIMESTAMPTZ '2026-10-18 00:00:00+00', NULL, TRUE, s.boundary, NOW(), 'system', NOW(), FALSE
FROM enugu_zone_seed s
ON CONFLICT ("CountryCode", "Code") WHERE "IsDeleted" = FALSE DO UPDATE
SET
    "Name" = EXCLUDED."Name",
    "Priority" = EXCLUDED."Priority",
    "Boundary" = EXCLUDED."Boundary",
    "UpdatedAtUtc" = NOW(),
    "UpdatedBy" = 'system'
WHERE deliveries."DeliveryZones"."Name" IS DISTINCT FROM EXCLUDED."Name"
   OR deliveries."DeliveryZones"."Priority" IS DISTINCT FROM EXCLUDED."Priority"
   OR ST_AsEWKB(deliveries."DeliveryZones"."Boundary") IS DISTINCT FROM ST_AsEWKB(EXCLUDED."Boundary");
