/*
    Enugu reference-data validation (read-only).

    Run after a deployment (PostDeploy 0002-0008):
      psql "$PICKLY_DB" -v ON_ERROR_STOP=1 -f enugu-validation.sql

    Prints one row per check (PASS / WARN / FAIL) and then raises an error, so psql exits non-zero, when any
    check FAILs. Only a session temp table is written; application data is never modified.
*/

CREATE TEMP TABLE enugu_validation (check_name text, status text, detail text);

/* Zones the ENU-V1 matrix is built for. */
CREATE TEMP VIEW enugu_zone AS
SELECT z.*
FROM deliveries."DeliveryZones" z
WHERE z."CountryCode" = 'NG' AND z."IsDeleted" = FALSE
  AND z."Code" IN ('ENU-ABK', 'ENU-ACH', 'ENU-EME', 'ENU-GRA', 'ENU-IND', 'ENU-NHV', 'ENU-OGU', 'ENU-TEK', 'ENU-THC', 'ENU-UWA');

CREATE TEMP VIEW active_zone AS
SELECT z.*
FROM deliveries."DeliveryZones" z
WHERE z."IsDeleted" = FALSE AND z."IsActive" = TRUE
  AND z."EffectiveFromUtc" <= NOW() AND (z."EffectiveToUtc" IS NULL OR z."EffectiveToUtc" > NOW());

/* Usable Enugu locations: everything seeded from OSM plus anything registered later in Enugu State. */
CREATE TEMP VIEW enugu_location AS
SELECT l.*, ST_SetSRID(l."Coordinates"::geometry, 4326) AS point
FROM locations."Locations" l
WHERE l."IsDeleted" = FALSE AND l."ResolutionStatus" = 'Resolved'
  AND (l."State" ILIKE 'Enugu%'
       OR EXISTS (SELECT 1 FROM locations."LocationProviderReferences" r WHERE r."LocationId" = l."Id" AND r."Provider" = 'openstreetmap'));

/* ---------------------------------------------------------------- Geography */

INSERT INTO enugu_validation
SELECT 'zones: 10 Enugu zones exist and are active now',
       CASE WHEN count(*) FILTER (WHERE a."Id" IS NOT NULL) = 10 THEN 'PASS' ELSE 'FAIL' END,
       count(*) || ' present, ' || count(*) FILTER (WHERE a."Id" IS NOT NULL) || ' active and effective'
FROM enugu_zone z LEFT JOIN active_zone a ON a."Id" = z."Id";

INSERT INTO enugu_validation
SELECT 'zones: valid MultiPolygon geometry, SRID 4326',
       CASE WHEN count(*) = 0 THEN 'PASS' ELSE 'FAIL' END,
       coalesce(string_agg("Code" || ' (' || ST_IsValidReason("Boundary") || ', ' || GeometryType("Boundary") || ', SRID ' || ST_SRID("Boundary") || ')', '; '), 'all valid')
FROM deliveries."DeliveryZones"
WHERE "IsDeleted" = FALSE AND (NOT ST_IsValid("Boundary") OR GeometryType("Boundary") <> 'MULTIPOLYGON' OR ST_SRID("Boundary") <> 4326);

INSERT INTO enugu_validation
SELECT 'zones: inside Enugu (no longitude/latitude reversal)',
       CASE WHEN count(*) = 0 THEN 'PASS' ELSE 'FAIL' END,
       coalesce(string_agg("Code", ', '), 'all inside 7.40..7.70 E, 6.30..6.70 N')
FROM enugu_zone
WHERE NOT ST_CoveredBy("Boundary", ST_MakeEnvelope(7.40, 6.30, 7.70, 6.70, 4326));

INSERT INTO enugu_validation
SELECT 'zones: no same-priority overlap between active zones',
       CASE WHEN count(*) = 0 THEN 'PASS' ELSE 'FAIL' END,
       coalesce(string_agg(a."Code" || ' / ' || b."Code", ', '), 'none')
FROM active_zone a JOIN active_zone b ON a."Id" < b."Id" AND a."Priority" = b."Priority"
WHERE ST_Intersects(a."Boundary", b."Boundary");

INSERT INTO enugu_validation
SELECT 'zones: overlapping active zones (resolved by priority)',
       CASE WHEN count(*) = 0 THEN 'PASS' ELSE 'WARN' END,
       coalesce(string_agg(a."Code" || ' / ' || b."Code", ', '), 'none; zones only share edges')
FROM active_zone a JOIN active_zone b ON a."Id" < b."Id"
WHERE ST_Area(ST_Intersection(a."Boundary", b."Boundary")) > 0;

INSERT INTO enugu_validation
SELECT 'locations: OSM-seeded streets and landmarks',
       CASE WHEN count(DISTINCT l."Id") FILTER (WHERE l."LocationType" = 'Street') >= 130
             AND count(DISTINCT l."Id") FILTER (WHERE l."LocationType" = 'Landmark') >= 94 THEN 'PASS' ELSE 'WARN' END,
       count(DISTINCT l."Id") FILTER (WHERE l."LocationType" = 'Street') || ' streets (130 seeded), '
         || count(DISTINCT l."Id") FILTER (WHERE l."LocationType" = 'Landmark') || ' landmarks (94 seeded); '
         || 'fewer means a row was skipped because an equivalent location already existed'
FROM locations."Locations" l
JOIN locations."LocationProviderReferences" r
  ON r."LocationId" = l."Id" AND r."Provider" = 'openstreetmap' AND r."IsDeleted" = FALSE
WHERE l."IsDeleted" = FALSE;

INSERT INTO enugu_validation
SELECT 'locations: Annunciation hospital canonical point and provenance',
       CASE WHEN count(*) = 1
                  AND bool_and(l."LocationType" = 'Landmark'
                      AND l."NormalizedAddress" = 'annunciation hospital|enugu|enugu|ng'
                      AND (l."ResolutionSource" = 'Mapbox'
                           OR (l."ResolutionSource" = 'InternallyRegistered'
                               AND ST_DWithin(l."Coordinates", ST_SetSRID(ST_MakePoint(7.5919103, 6.4684403), 4326)::geography, 1))))
            THEN 'PASS' ELSE 'FAIL' END,
       count(*) || ' matching node/8753751530 reference(s)'
FROM locations."LocationProviderReferences" r
JOIN locations."Locations" l ON l."Id" = r."LocationId" AND l."IsDeleted" = FALSE
WHERE r."Provider" = 'openstreetmap' AND r."FeatureId" = 'node/8753751530' AND r."IsDeleted" = FALSE;

INSERT INTO enugu_validation
SELECT 'locations: connected Annunciation road segments share one canonical street',
       CASE WHEN count(DISTINCT r."FeatureId") = 2 AND count(DISTINCT r."LocationId") = 1
            THEN 'PASS' ELSE 'FAIL' END,
       count(DISTINCT r."FeatureId") || ' references mapped to ' || count(DISTINCT r."LocationId") || ' location(s)'
FROM locations."LocationProviderReferences" r
JOIN locations."Locations" l ON l."Id" = r."LocationId"
WHERE r."Provider" = 'openstreetmap'
  AND r."FeatureId" IN ('way/241655401', 'way/241655569')
  AND r."IsDeleted" = FALSE AND l."IsDeleted" = FALSE
  AND l."NormalizedAddress" = 'annunciation hospital road|enugu|enugu|ng'
  AND l."LocationType" = 'Street';

INSERT INTO enugu_validation
SELECT 'locations: no duplicate canonical identities',
       CASE WHEN count(*) = 0 THEN 'PASS' ELSE 'FAIL' END,
       coalesce(string_agg(identity, '; '), 'none')
FROM (
    SELECT "NormalizedAddress" || ' x' || count(*) AS identity
    FROM enugu_location GROUP BY "NormalizedAddress", "LocationType" HAVING count(*) > 1
) d;

INSERT INTO enugu_validation
SELECT 'locations: coordinates inside Enugu',
       CASE WHEN count(*) = 0 THEN 'PASS' ELSE 'FAIL' END,
       coalesce(string_agg("DisplayAddress", '; '), 'all inside 7.40..7.70 E, 6.30..6.70 N')
FROM enugu_location
WHERE NOT ST_CoveredBy(point, ST_MakeEnvelope(7.40, 6.30, 7.70, 6.70, 4326));

INSERT INTO enugu_validation
SELECT 'locations: covered by an active zone',
       CASE WHEN count(*) = 0 THEN 'PASS' ELSE 'FAIL' END,
       coalesce(count(*) || ': ' || string_agg("DisplayAddress", '; '), 'all covered')
FROM enugu_location l
WHERE NOT EXISTS (SELECT 1 FROM active_zone z WHERE ST_Covers(z."Boundary", l.point));

INSERT INTO enugu_validation
SELECT 'locations: unambiguous winning zone',
       CASE WHEN count(*) = 0 THEN 'PASS' ELSE 'FAIL' END,
       coalesce(string_agg("DisplayAddress", '; '), 'every covered location has one top-priority zone')
FROM enugu_location l
WHERE (SELECT count(*) FROM active_zone z
       WHERE ST_Covers(z."Boundary", l.point)
         AND z."Priority" = (SELECT max(z2."Priority") FROM active_zone z2 WHERE ST_Covers(z2."Boundary", l.point))) > 1;

/* ---------------------------------------------------------------- Vehicles */

INSERT INTO enugu_validation
SELECT 'vehicles: MOTORCYCLE 10 kg, CAR 50 kg, VAN 300 kg active',
       CASE WHEN count(v."Id") = 3 AND bool_and(v."IsActive" AND v."MaximumWeightKg" = e.kg) THEN 'PASS' ELSE 'FAIL' END,
       string_agg(e.code || '=' || coalesce(v."MaximumWeightKg"::text || ' kg' || CASE WHEN v."IsActive" THEN '' ELSE ' INACTIVE' END, 'MISSING'), ', ')
FROM (VALUES ('MOTORCYCLE', 10.000), ('CAR', 50.000), ('VAN', 300.000)) AS e(code, kg)
LEFT JOIN deliveries."VehicleCategories" v ON v."Code" = e.code AND v."IsDeleted" = FALSE;

INSERT INTO enugu_validation
SELECT 'vehicles: official Mapbox routing profiles',
       CASE WHEN count(*) = 0 THEN 'PASS' ELSE 'FAIL' END,
       coalesce(string_agg("Code" || '=' || "RoutingProfile", ', '), 'all driving / driving-traffic / walking / cycling')
FROM deliveries."VehicleCategories"
WHERE "IsDeleted" = FALSE AND "RoutingProfile" NOT IN ('driving', 'driving-traffic', 'walking', 'cycling');

/* ---------------------------------------------------------------- Pricing */

INSERT INTO enugu_validation
SELECT 'pricing: NGN currency active',
       CASE WHEN count(*) = 1 THEN 'PASS' ELSE 'FAIL' END, count(*) || ' active NGN row(s)'
FROM payments."Currencies" WHERE "CurrencyCode" = 'NGN' AND "IsActive" = TRUE AND "IsDeleted" = FALSE;

INSERT INTO enugu_validation
SELECT 'pricing: ENU-V1 exists, active, effective now, NGN',
       CASE WHEN count(*) = 1 AND bool_and(p."IsActive" AND p."EffectiveFromUtc" <= NOW()
                 AND (p."EffectiveToUtc" IS NULL OR p."EffectiveToUtc" > NOW()) AND c."CurrencyCode" = 'NGN')
            THEN 'PASS' ELSE 'FAIL' END,
       coalesce(string_agg('active=' || p."IsActive" || ', from ' || p."EffectiveFromUtc" || ', to ' || coalesce(p."EffectiveToUtc"::text, 'open') || ', ' || c."CurrencyCode", '; '), 'ENU-V1 missing')
FROM deliveries."DeliveryPricingPolicies" p
JOIN payments."Currencies" c ON c."Id" = p."CurrencyId"
WHERE p."Version" = 'ENU-V1' AND p."IsDeleted" = FALSE;

INSERT INTO enugu_validation
SELECT 'pricing: ENU-V1 base 1200, 250/km, 50/kg, all deliveries',
       CASE WHEN count(*) = 1 AND bool_and("BaseFee" = 1200 AND "DistanceRatePerKilometre" = 250 AND "WeightRatePerKilogram" = 50
                 AND "DeliveryType" IS NULL AND "OrderType" IS NULL) THEN 'PASS' ELSE 'FAIL' END,
       coalesce(string_agg("BaseFee" || ' / ' || "DistanceRatePerKilometre" || ' / ' || "WeightRatePerKilogram"
                || ', deliveryType=' || coalesce("DeliveryType"::text, 'any') || ', orderType=' || coalesce("OrderType"::text, 'any'), '; '), 'ENU-V1 missing')
FROM deliveries."DeliveryPricingPolicies" WHERE "Version" = 'ENU-V1' AND "IsDeleted" = FALSE;

INSERT INTO enugu_validation
SELECT 'pricing: ENU-V1 vehicle rules MOTORCYCLE 0, CAR 1500, VAN 3500',
       CASE WHEN count(r."Id") = 3 AND bool_and(r."Charge" = e.charge) THEN 'PASS' ELSE 'FAIL' END,
       string_agg(e.code || '=' || coalesce(r."Charge"::text, 'MISSING'), ', ')
FROM (VALUES ('MOTORCYCLE', 0.00), ('CAR', 1500.00), ('VAN', 3500.00)) AS e(code, charge)
LEFT JOIN deliveries."VehicleCategories" v ON v."Code" = e.code AND v."IsDeleted" = FALSE
LEFT JOIN deliveries."DeliveryPricingPolicies" p ON p."Version" = 'ENU-V1' AND p."IsDeleted" = FALSE
LEFT JOIN deliveries."DeliveryVehiclePricingRules" r ON r."DeliveryPricingPolicyId" = p."Id" AND r."VehicleCategoryId" = v."Id" AND r."IsDeleted" = FALSE;

INSERT INTO enugu_validation
SELECT 'pricing: ENU-V1 zone matrix complete (N x N directional)',
       CASE WHEN count(*) FILTER (WHERE r."Id" IS NULL) = 0 AND count(*) = 100 THEN 'PASS' ELSE 'FAIL' END,
       (count(*) - count(*) FILTER (WHERE r."Id" IS NULL)) || ' of ' || count(*) || ' pairs priced'
         || coalesce('; missing ' || string_agg(o."Code" || '->' || d."Code", ', ') FILTER (WHERE r."Id" IS NULL), '')
FROM enugu_zone o CROSS JOIN enugu_zone d
LEFT JOIN deliveries."DeliveryPricingPolicies" p ON p."Version" = 'ENU-V1' AND p."IsDeleted" = FALSE
LEFT JOIN deliveries."DeliveryZonePricingRules" r
       ON r."DeliveryPricingPolicyId" = p."Id" AND r."OriginZoneId" = o."Id" AND r."DestinationZoneId" = d."Id" AND r."IsDeleted" = FALSE;

INSERT INTO enugu_validation
SELECT 'pricing: no duplicate directional rules',
       CASE WHEN count(*) = 0 THEN 'PASS' ELSE 'FAIL' END, coalesce(string_agg(pair, ', '), 'none')
FROM (
    SELECT p."Version" || ':' || r."OriginZoneId" || '->' || r."DestinationZoneId" AS pair
    FROM deliveries."DeliveryZonePricingRules" r JOIN deliveries."DeliveryPricingPolicies" p ON p."Id" = r."DeliveryPricingPolicyId"
    WHERE r."IsDeleted" = FALSE
    GROUP BY p."Version", r."OriginZoneId", r."DestinationZoneId" HAVING count(*) > 1
) d;

/* Cross-checks the tariff against the live polygons: zones sharing an edge are adjacent. */
INSERT INTO enugu_validation
SELECT 'pricing: same zone 0, adjacent 200, other pairs 400-600',
       CASE WHEN count(*) = 0 THEN 'PASS' ELSE 'FAIL' END,
       coalesce(count(*) || ' rule(s) outside the tariff: ' || string_agg(pair, ', '), 'all 100 rules match the tariff band')
FROM (
    SELECT o."Code" || '->' || d."Code" || '=' || r."Charge" AS pair, o."Id" = d."Id" AS same,
           ST_Length(ST_Intersection(ST_Boundary(o."Boundary"), ST_Boundary(d."Boundary"))) > 0 AS adjacent, r."Charge"
    FROM deliveries."DeliveryZonePricingRules" r
    JOIN deliveries."DeliveryPricingPolicies" p ON p."Id" = r."DeliveryPricingPolicyId" AND p."Version" = 'ENU-V1' AND p."IsDeleted" = FALSE
    JOIN enugu_zone o ON o."Id" = r."OriginZoneId"
    JOIN enugu_zone d ON d."Id" = r."DestinationZoneId"
    WHERE r."IsDeleted" = FALSE
) x
WHERE NOT ((same AND "Charge" = 0) OR (NOT same AND adjacent AND "Charge" = 200) OR (NOT same AND NOT adjacent AND "Charge" BETWEEN 400 AND 600));

INSERT INTO enugu_validation
SELECT 'pricing: one complete policy per vehicle and zone pair now',
       CASE WHEN count(*) = 0 THEN 'PASS' ELSE 'FAIL' END,
       coalesce(count(*) || ' ambiguous combination(s), e.g. ' || min(combo), 'no ambiguity')
FROM (
    SELECT vr."VehicleCategoryId" || ' ' || zr."OriginZoneId" || '->' || zr."DestinationZoneId" AS combo
    FROM deliveries."DeliveryPricingPolicies" p
    JOIN deliveries."DeliveryVehiclePricingRules" vr ON vr."DeliveryPricingPolicyId" = p."Id" AND vr."IsDeleted" = FALSE
    JOIN deliveries."DeliveryZonePricingRules" zr ON zr."DeliveryPricingPolicyId" = p."Id" AND zr."IsDeleted" = FALSE
    WHERE p."IsDeleted" = FALSE AND p."IsActive" = TRUE
      AND p."EffectiveFromUtc" <= NOW() AND (p."EffectiveToUtc" IS NULL OR p."EffectiveToUtc" > NOW())
    GROUP BY vr."VehicleCategoryId", zr."OriginZoneId", zr."DestinationZoneId",
             coalesce(p."DeliveryType", 0), coalesce(p."OrderType", 0)
    HAVING count(*) > 1
) a;

SELECT status, check_name, detail FROM enugu_validation ORDER BY CASE status WHEN 'FAIL' THEN 0 WHEN 'WARN' THEN 1 ELSE 2 END, check_name;

DO $$
DECLARE
    failures text;
BEGIN
    SELECT string_agg(check_name, '; ') INTO failures FROM enugu_validation WHERE status = 'FAIL';
    IF failures IS NOT NULL THEN
        RAISE EXCEPTION 'Enugu validation failed: %', failures;
    END IF;
    RAISE NOTICE 'Enugu validation passed.';
END $$;
