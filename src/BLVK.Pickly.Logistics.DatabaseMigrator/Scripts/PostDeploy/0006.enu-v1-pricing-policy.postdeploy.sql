/*
    ENU-V1 delivery pricing policy for Enugu (NGN).

    Approved tariff:
      base fee N1,200; distance N250/km (Mapbox route distance); weight N50/kg;
      vehicle charge MOTORCYCLE N0, CAR N1,500, VAN N3,500;
      zone charge same zone N0, adjacent zones N200.
    PENDING APPROVAL: non-adjacent zone pairs. The approved band is N400-N600; this proposal charges N400 for
    pairs two zone-hops apart and N600 for pairs three hops apart (hops counted over shared zone edges of the
    draft polygons in 0004.enugu-delivery-zones.postdeploy.sql). Review the "relationship" column below; once
    ENU-V1 exists in an environment its charges can only change through a new version.

    Total = BaseFee + DistanceCharge + WeightCharge + VehicleCharge + ZoneCharge, computed by the existing
    DeliveryPricingCalculator (two-decimal rounding away from zero). The matrix is directional and complete:
    10 zones => 100 (origin, destination) rules including same-zone routes; A->B and B->A are separate rows.

    Runs on every deployment, after 0003 (vehicle categories) and 0004 (zones), as one batch and therefore one
    transaction.

    Idempotent: a pricing version is immutable once created, because quotes and deliveries reference it.
    If ENU-V1 already exists the script changes nothing (Diagnostics/enugu-validation.sql reports drift). A tariff
    change is a new version (ENU-V2) with its own effective period, never an edit of ENU-V1. The policy, its 3
    vehicle rules and 100 zone rules are inserted together or not at all; missing prerequisites, an incomplete
    matrix or an overlapping active policy abort the transaction.
*/

SELECT pg_advisory_xact_lock(hashtext('pickly.seed.enu-v1-pricing-policy'));

CREATE TEMP TABLE enu_v1_vehicle_rules ON COMMIT DROP AS
SELECT * FROM (VALUES
    ('MOTORCYCLE', 0.00),
    ('CAR', 1500.00),
    ('VAN', 3500.00)
) AS v(vehicle_code, charge);

CREATE TEMP TABLE enu_v1_zone_rules ON COMMIT DROP AS
SELECT * FROM (VALUES
    ('ENU-ABK', 'ENU-ABK', 'same-zone', 0.00),
    ('ENU-ABK', 'ENU-ACH', 'three-hop', 600.00),
    ('ENU-ABK', 'ENU-EME', 'adjacent', 200.00),
    ('ENU-ABK', 'ENU-GRA', 'two-hop', 400.00),
    ('ENU-ABK', 'ENU-IND', 'two-hop', 400.00),
    ('ENU-ABK', 'ENU-NHV', 'two-hop', 400.00),
    ('ENU-ABK', 'ENU-OGU', 'three-hop', 600.00),
    ('ENU-ABK', 'ENU-TEK', 'adjacent', 200.00),
    ('ENU-ABK', 'ENU-THC', 'adjacent', 200.00),
    ('ENU-ABK', 'ENU-UWA', 'three-hop', 600.00),
    ('ENU-ACH', 'ENU-ABK', 'three-hop', 600.00),
    ('ENU-ACH', 'ENU-ACH', 'same-zone', 0.00),
    ('ENU-ACH', 'ENU-EME', 'three-hop', 600.00),
    ('ENU-ACH', 'ENU-GRA', 'adjacent', 200.00),
    ('ENU-ACH', 'ENU-IND', 'two-hop', 400.00),
    ('ENU-ACH', 'ENU-NHV', 'two-hop', 400.00),
    ('ENU-ACH', 'ENU-OGU', 'adjacent', 200.00),
    ('ENU-ACH', 'ENU-TEK', 'two-hop', 400.00),
    ('ENU-ACH', 'ENU-THC', 'two-hop', 400.00),
    ('ENU-ACH', 'ENU-UWA', 'adjacent', 200.00),
    ('ENU-EME', 'ENU-ABK', 'adjacent', 200.00),
    ('ENU-EME', 'ENU-ACH', 'three-hop', 600.00),
    ('ENU-EME', 'ENU-EME', 'same-zone', 0.00),
    ('ENU-EME', 'ENU-GRA', 'two-hop', 400.00),
    ('ENU-EME', 'ENU-IND', 'adjacent', 200.00),
    ('ENU-EME', 'ENU-NHV', 'adjacent', 200.00),
    ('ENU-EME', 'ENU-OGU', 'two-hop', 400.00),
    ('ENU-EME', 'ENU-TEK', 'two-hop', 400.00),
    ('ENU-EME', 'ENU-THC', 'adjacent', 200.00),
    ('ENU-EME', 'ENU-UWA', 'two-hop', 400.00),
    ('ENU-GRA', 'ENU-ABK', 'two-hop', 400.00),
    ('ENU-GRA', 'ENU-ACH', 'adjacent', 200.00),
    ('ENU-GRA', 'ENU-EME', 'two-hop', 400.00),
    ('ENU-GRA', 'ENU-GRA', 'same-zone', 0.00),
    ('ENU-GRA', 'ENU-IND', 'two-hop', 400.00),
    ('ENU-GRA', 'ENU-NHV', 'adjacent', 200.00),
    ('ENU-GRA', 'ENU-OGU', 'adjacent', 200.00),
    ('ENU-GRA', 'ENU-TEK', 'adjacent', 200.00),
    ('ENU-GRA', 'ENU-THC', 'adjacent', 200.00),
    ('ENU-GRA', 'ENU-UWA', 'two-hop', 400.00),
    ('ENU-IND', 'ENU-ABK', 'two-hop', 400.00),
    ('ENU-IND', 'ENU-ACH', 'two-hop', 400.00),
    ('ENU-IND', 'ENU-EME', 'adjacent', 200.00),
    ('ENU-IND', 'ENU-GRA', 'two-hop', 400.00),
    ('ENU-IND', 'ENU-IND', 'same-zone', 0.00),
    ('ENU-IND', 'ENU-NHV', 'adjacent', 200.00),
    ('ENU-IND', 'ENU-OGU', 'adjacent', 200.00),
    ('ENU-IND', 'ENU-TEK', 'three-hop', 600.00),
    ('ENU-IND', 'ENU-THC', 'two-hop', 400.00),
    ('ENU-IND', 'ENU-UWA', 'adjacent', 200.00),
    ('ENU-NHV', 'ENU-ABK', 'two-hop', 400.00),
    ('ENU-NHV', 'ENU-ACH', 'two-hop', 400.00),
    ('ENU-NHV', 'ENU-EME', 'adjacent', 200.00),
    ('ENU-NHV', 'ENU-GRA', 'adjacent', 200.00),
    ('ENU-NHV', 'ENU-IND', 'adjacent', 200.00),
    ('ENU-NHV', 'ENU-NHV', 'same-zone', 0.00),
    ('ENU-NHV', 'ENU-OGU', 'adjacent', 200.00),
    ('ENU-NHV', 'ENU-TEK', 'two-hop', 400.00),
    ('ENU-NHV', 'ENU-THC', 'adjacent', 200.00),
    ('ENU-NHV', 'ENU-UWA', 'two-hop', 400.00),
    ('ENU-OGU', 'ENU-ABK', 'three-hop', 600.00),
    ('ENU-OGU', 'ENU-ACH', 'adjacent', 200.00),
    ('ENU-OGU', 'ENU-EME', 'two-hop', 400.00),
    ('ENU-OGU', 'ENU-GRA', 'adjacent', 200.00),
    ('ENU-OGU', 'ENU-IND', 'adjacent', 200.00),
    ('ENU-OGU', 'ENU-NHV', 'adjacent', 200.00),
    ('ENU-OGU', 'ENU-OGU', 'same-zone', 0.00),
    ('ENU-OGU', 'ENU-TEK', 'two-hop', 400.00),
    ('ENU-OGU', 'ENU-THC', 'two-hop', 400.00),
    ('ENU-OGU', 'ENU-UWA', 'adjacent', 200.00),
    ('ENU-TEK', 'ENU-ABK', 'adjacent', 200.00),
    ('ENU-TEK', 'ENU-ACH', 'two-hop', 400.00),
    ('ENU-TEK', 'ENU-EME', 'two-hop', 400.00),
    ('ENU-TEK', 'ENU-GRA', 'adjacent', 200.00),
    ('ENU-TEK', 'ENU-IND', 'three-hop', 600.00),
    ('ENU-TEK', 'ENU-NHV', 'two-hop', 400.00),
    ('ENU-TEK', 'ENU-OGU', 'two-hop', 400.00),
    ('ENU-TEK', 'ENU-TEK', 'same-zone', 0.00),
    ('ENU-TEK', 'ENU-THC', 'adjacent', 200.00),
    ('ENU-TEK', 'ENU-UWA', 'three-hop', 600.00),
    ('ENU-THC', 'ENU-ABK', 'adjacent', 200.00),
    ('ENU-THC', 'ENU-ACH', 'two-hop', 400.00),
    ('ENU-THC', 'ENU-EME', 'adjacent', 200.00),
    ('ENU-THC', 'ENU-GRA', 'adjacent', 200.00),
    ('ENU-THC', 'ENU-IND', 'two-hop', 400.00),
    ('ENU-THC', 'ENU-NHV', 'adjacent', 200.00),
    ('ENU-THC', 'ENU-OGU', 'two-hop', 400.00),
    ('ENU-THC', 'ENU-TEK', 'adjacent', 200.00),
    ('ENU-THC', 'ENU-THC', 'same-zone', 0.00),
    ('ENU-THC', 'ENU-UWA', 'three-hop', 600.00),
    ('ENU-UWA', 'ENU-ABK', 'three-hop', 600.00),
    ('ENU-UWA', 'ENU-ACH', 'adjacent', 200.00),
    ('ENU-UWA', 'ENU-EME', 'two-hop', 400.00),
    ('ENU-UWA', 'ENU-GRA', 'two-hop', 400.00),
    ('ENU-UWA', 'ENU-IND', 'adjacent', 200.00),
    ('ENU-UWA', 'ENU-NHV', 'two-hop', 400.00),
    ('ENU-UWA', 'ENU-OGU', 'adjacent', 200.00),
    ('ENU-UWA', 'ENU-TEK', 'three-hop', 600.00),
    ('ENU-UWA', 'ENU-THC', 'three-hop', 600.00),
    ('ENU-UWA', 'ENU-UWA', 'same-zone', 0.00)
) AS v(origin_code, destination_code, relationship, charge);

DO $$
DECLARE
    effective_from CONSTANT timestamptz := TIMESTAMPTZ '2026-10-18 00:00:00+00';
    ngn_id uuid;
    policy_id uuid := gen_random_uuid();
    zone_codes text[];
    problems text;
BEGIN
    IF EXISTS (SELECT 1 FROM deliveries."DeliveryPricingPolicies" WHERE "Version" = 'ENU-V1' AND "IsDeleted" = FALSE) THEN
        RAISE NOTICE 'ENU-V1 already exists. Pricing versions are immutable; nothing was changed.';
        RETURN;
    END IF;

    SELECT c."Id" INTO ngn_id
    FROM payments."Currencies" c
    WHERE c."CurrencyCode" = 'NGN' AND c."IsActive" = TRUE AND c."IsDeleted" = FALSE;
    IF ngn_id IS NULL THEN
        RAISE EXCEPTION 'ENU-V1 rejected: active NGN currency is missing. Run the DatabaseMigrator (0001 template seeder) first.';
    END IF;

    SELECT array_agg(DISTINCT origin_code ORDER BY origin_code) INTO zone_codes FROM enu_v1_zone_rules;

    SELECT string_agg(issue, '; ') INTO problems
    FROM (
        SELECT 'vehicle category ' || r.vehicle_code || ' is missing or inactive' AS issue
        FROM enu_v1_vehicle_rules r
        WHERE NOT EXISTS (
            SELECT 1 FROM deliveries."VehicleCategories" vc
            WHERE vc."Code" = r.vehicle_code AND vc."IsActive" = TRUE AND vc."IsDeleted" = FALSE)
        UNION ALL
        SELECT 'delivery zone ' || zone_code || ' is missing or inactive'
        FROM unnest(zone_codes) AS zone_code
        WHERE NOT EXISTS (
            SELECT 1 FROM deliveries."DeliveryZones" z
            WHERE z."CountryCode" = 'NG' AND z."Code" = zone_code AND z."IsActive" = TRUE AND z."IsDeleted" = FALSE)
        UNION ALL
        SELECT 'matrix has ' || count(*) || ' rules; ' || cardinality(zone_codes) * cardinality(zone_codes) || ' required'
        FROM enu_v1_zone_rules HAVING count(*) <> cardinality(zone_codes) * cardinality(zone_codes)
        UNION ALL
        SELECT 'matrix is missing ' || o || ' -> ' || d
        FROM unnest(zone_codes) AS o CROSS JOIN unnest(zone_codes) AS d
        WHERE NOT EXISTS (SELECT 1 FROM enu_v1_zone_rules r WHERE r.origin_code = o AND r.destination_code = d)
        UNION ALL
        SELECT 'matrix duplicates ' || origin_code || ' -> ' || destination_code
        FROM enu_v1_zone_rules GROUP BY origin_code, destination_code HAVING count(*) > 1
        UNION ALL
        SELECT 'same-zone charge for ' || origin_code || ' must be 0'
        FROM enu_v1_zone_rules WHERE origin_code = destination_code AND charge <> 0
        UNION ALL
        SELECT 'invalid charge for ' || origin_code || ' -> ' || destination_code
        FROM enu_v1_zone_rules WHERE charge < 0 OR round(charge, 2) <> charge
        UNION ALL
        SELECT 'active policy ' || p."Version" || ' overlaps ENU-V1 for Enugu routes and would make pricing ambiguous'
        FROM deliveries."DeliveryPricingPolicies" p
        WHERE p."IsDeleted" = FALSE AND p."IsActive" = TRUE
          AND (p."EffectiveToUtc" IS NULL OR p."EffectiveToUtc" > effective_from)
          AND EXISTS (
              SELECT 1 FROM deliveries."DeliveryZonePricingRules" zr
              JOIN deliveries."DeliveryZones" z ON z."Id" = zr."OriginZoneId"
              WHERE zr."DeliveryPricingPolicyId" = p."Id" AND zr."IsDeleted" = FALSE AND z."Code" = ANY (zone_codes))
    ) issues;

    IF problems IS NOT NULL THEN
        RAISE EXCEPTION 'ENU-V1 rejected: %', problems;
    END IF;

    INSERT INTO deliveries."DeliveryPricingPolicies" ("Id", "Version", "CurrencyId", "BaseFee", "DistanceRatePerKilometre", "WeightRatePerKilogram", "EffectiveFromUtc", "EffectiveToUtc", "DeliveryType", "OrderType", "IsActive", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "IsDeleted")
    VALUES (policy_id, 'ENU-V1', ngn_id, 1200.00, 250.000000, 50.000000, effective_from, NULL, NULL, NULL, TRUE, NOW(), 'system', NOW(), FALSE);

    INSERT INTO deliveries."DeliveryVehiclePricingRules" ("Id", "DeliveryPricingPolicyId", "VehicleCategoryId", "Charge", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "IsDeleted")
    SELECT gen_random_uuid(), policy_id, vc."Id", r.charge, NOW(), 'system', NOW(), FALSE
    FROM enu_v1_vehicle_rules r
    JOIN deliveries."VehicleCategories" vc ON vc."Code" = r.vehicle_code AND vc."IsDeleted" = FALSE;

    INSERT INTO deliveries."DeliveryZonePricingRules" ("Id", "DeliveryPricingPolicyId", "OriginZoneId", "DestinationZoneId", "Charge", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "IsDeleted")
    SELECT gen_random_uuid(), policy_id, o."Id", d."Id", r.charge, NOW(), 'system', NOW(), FALSE
    FROM enu_v1_zone_rules r
    JOIN deliveries."DeliveryZones" o ON o."CountryCode" = 'NG' AND o."Code" = r.origin_code AND o."IsDeleted" = FALSE
    JOIN deliveries."DeliveryZones" d ON d."CountryCode" = 'NG' AND d."Code" = r.destination_code AND d."IsDeleted" = FALSE;

    RAISE NOTICE 'ENU-V1 created with % vehicle rules and % zone rules.',
        (SELECT count(*) FROM enu_v1_vehicle_rules), (SELECT count(*) FROM enu_v1_zone_rules);
END $$;
