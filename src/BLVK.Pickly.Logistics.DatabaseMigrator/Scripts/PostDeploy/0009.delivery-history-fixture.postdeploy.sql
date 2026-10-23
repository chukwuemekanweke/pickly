/*
    Believable two-week delivery history for the MVP rider roster.

    The fixture represents a small pilot operation, not a mature high-volume fleet:
      - five completed Pay On Delivery jobs per approved test rider plus 20 additional completed jobs
        (normally 105 rows), spread over 14 days;
      - four recently assigned jobs, one each for four riders, so active-delivery screens remain useful;
      - one demo business sender owning the fixture quotes and deliveries.

    Before adding the fixture, every non-fixture RiderAssigned/PickedUp/InTransit delivery is completed. Missing
    cash-collection and proof references are supplied where required, and append-only lifecycle history is recorded.
    A successful run leaves durable fixture provenance. Later deployments still invoke this file because post-deploy
    scripts are unjournaled, but the provenance guard makes them no-ops with no persistent data changes.

    Runs after 0005-0008 and expects the Enugu locations, ENU-V1 pricing policy and test rider roster. Every created
    row is tagged 'system:delivery-history-fixture'.
*/

SELECT pg_advisory_xact_lock(hashtext('pickly.seed.delivery-history-fixture'));

CREATE TEMP TABLE delivery_history_fixture_execution ON COMMIT DROP AS
SELECT NOT EXISTS (
    SELECT 1
    FROM deliveries."Deliveries" d
    WHERE d."CreatedBy" = 'system:delivery-history-fixture'
    UNION ALL
    SELECT 1
    FROM authentication."Users" u
    WHERE u."NormalizedEmail" = 'PIKLYTECH+DELIVERYHISTORY@GMAIL.COM'
      AND u."CreatedBy" = 'system:delivery-history-fixture'
) AS should_run;

CREATE TEMP TABLE active_delivery_completion ON COMMIT DROP AS
SELECT
    d."Id" AS delivery_id,
    d."Status" AS original_status,
    d."OrderType" AS order_type,
    COALESCE(d."CashCollectionId", gen_random_uuid()) AS cash_collection_id,
    COALESCE(d."ProofOfDeliveryId", gen_random_uuid()) AS proof_of_delivery_id,
    NOW() AS completed_at_utc
FROM deliveries."Deliveries" d
WHERE d."Status" IN (4, 5, 6)
  AND d."IsDeleted" = FALSE
  AND d."CreatedBy" IS DISTINCT FROM 'system:delivery-history-fixture'
  AND (SELECT should_run FROM delivery_history_fixture_execution);

INSERT INTO deliveries."DeliveryStatusHistory"
    ("Id", "DeliveryId", "PreviousStatus", "NewStatus", "Reason", "OccurredAtUtc", "Actor", "Source",
     "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy", "IsDeleted")
SELECT
    gen_random_uuid(), c.delivery_id, step.previous_status, step.new_status, step.reason,
    c.completed_at_utc + step.sequence_no * INTERVAL '1 microsecond',
    'system:delivery-history-fixture', 'delivery_history_reconciliation',
    c.completed_at_utc, 'system:delivery-history-fixture', c.completed_at_utc,
    'system:delivery-history-fixture', FALSE
FROM active_delivery_completion c
CROSS JOIN LATERAL (VALUES
    (4, 5, 'Parcels picked up.', 1),
    (5, 6, 'Delivery in transit.', 2),
    (6, 7, 'Delivery completed.', 3)
) AS step(previous_status, new_status, reason, sequence_no)
WHERE step.previous_status >= c.original_status;

UPDATE deliveries."Deliveries" d
SET
    "Status" = 7,
    "PaymentStatus" = 3,
    "CashCollectionId" = CASE WHEN c.order_type = 2 THEN c.cash_collection_id ELSE d."CashCollectionId" END,
    "PaymentSatisfiedAtUtc" = COALESCE(d."PaymentSatisfiedAtUtc", c.completed_at_utc),
    "ProofOfDeliveryId" = c.proof_of_delivery_id,
    "DeliveredAtUtc" = c.completed_at_utc + INTERVAL '3 microseconds',
    "UpdatedAtUtc" = c.completed_at_utc,
    "UpdatedBy" = 'system:delivery-history-fixture'
FROM active_delivery_completion c
WHERE d."Id" = c.delivery_id;

DO $$
DECLARE
    fixture_actor CONSTANT text := 'system:delivery-history-fixture';
    demo_email CONSTANT text := 'piklytech+deliveryhistory@gmail.com';
    default_tenant_id CONSTANT uuid := '1203d9d1-2a6b-48ef-9cc1-e561a23aff72';
    default_password_hash CONSTANT text := 'AQAAAAIAAYagAAAAEMGNDYUsmtS/QXeD3mbI8tW6BymrqtIiJ5bR9i/qNaqHOLNXvIG9xIvEusB1gWNIDg==';
    nigeria_id uuid;
    customer_type_id uuid;
    demo_user_id uuid;
    demo_sender_id uuid;
BEGIN
    IF NOT (SELECT should_run FROM delivery_history_fixture_execution) THEN
        RETURN;
    END IF;

    SELECT c."Id" INTO nigeria_id
    FROM reference_data."Countries" c
    WHERE c."ShortCode" = 'NG' AND c."IsDeleted" = FALSE;

    SELECT t."Id" INTO customer_type_id
    FROM stakeholders."StakeholderTypes" t
    WHERE t."TenantId" = default_tenant_id AND t."Key" = 'customer' AND t."IsDeleted" = FALSE;

    IF nigeria_id IS NULL OR customer_type_id IS NULL THEN
        RAISE EXCEPTION 'Delivery history fixture rejected: NG country or default customer stakeholder type is missing.';
    END IF;

    INSERT INTO authentication."Users"
        ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash",
         "SecurityStamp", "ConcurrencyStamp", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
         "AccessFailedCount", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy", "IsDeleted")
    SELECT
        gen_random_uuid(), demo_email, upper(demo_email), demo_email, upper(demo_email), TRUE,
        default_password_hash, upper(replace(gen_random_uuid()::text, '-', '')), gen_random_uuid()::text,
        FALSE, FALSE, TRUE, 0, NOW(), fixture_actor, NOW(), fixture_actor, FALSE
    WHERE NOT EXISTS (
        SELECT 1 FROM authentication."Users" u
        WHERE u."NormalizedEmail" = upper(demo_email) AND u."IsDeleted" = FALSE);

    SELECT u."Id" INTO demo_user_id
    FROM authentication."Users" u
    WHERE u."NormalizedEmail" = upper(demo_email) AND u."IsDeleted" = FALSE;

    INSERT INTO stakeholders."Stakeholders"
        ("Id", "AppUserId", "TenantId", "CountryId", "StakeholderTypeId", "FirstName", "LastName", "IsVerified",
         "Language", "Theme", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy", "IsDeleted")
    SELECT
        gen_random_uuid(), demo_user_id, default_tenant_id, nigeria_id, customer_type_id,
        'Pikly', 'Demo Operations', TRUE, 'en', 'system', NOW(), fixture_actor, NOW(), fixture_actor, FALSE
    WHERE NOT EXISTS (
        SELECT 1 FROM stakeholders."Stakeholders" s
        WHERE s."AppUserId" = demo_user_id AND s."IsDeleted" = FALSE);

    SELECT s."Id" INTO demo_sender_id
    FROM stakeholders."Stakeholders" s
    WHERE s."AppUserId" = demo_user_id AND s."IsDeleted" = FALSE;

    IF demo_sender_id IS NULL OR NOT EXISTS (
        SELECT 1
        FROM stakeholders."Stakeholders" s
        JOIN stakeholders."StakeholderTypes" t ON t."Id" = s."StakeholderTypeId"
        WHERE s."Id" = demo_sender_id AND t."Key" = 'customer' AND t."IsDeleted" = FALSE)
    THEN
        RAISE EXCEPTION 'Delivery history fixture rejected: demo sender account is not an active customer.';
    END IF;
END $$;

CREATE TEMP TABLE delivery_fixture_riders ON COMMIT DROP AS
SELECT
    row_number() OVER (ORDER BY u."NormalizedEmail")::integer AS rider_no,
    s."Id" AS rider_id,
    rp."VehicleCategoryId" AS vehicle_category_id,
    vc."Code" AS vehicle_category_code,
    vc."Name" AS vehicle_category_name
FROM authentication."Users" u
JOIN stakeholders."Stakeholders" s ON s."AppUserId" = u."Id" AND s."IsDeleted" = FALSE
JOIN riders."RiderProfiles" rp ON rp."StakeholderId" = s."Id" AND rp."IsDeleted" = FALSE
JOIN deliveries."VehicleCategories" vc ON vc."Id" = rp."VehicleCategoryId" AND vc."IsDeleted" = FALSE
WHERE u."NormalizedEmail" LIKE 'PIKLYTECH+RIDER%@GMAIL.COM'
  AND u."IsDeleted" = FALSE
  AND rp."VerificationStatus" = 2;

DO $$
BEGIN
    IF (SELECT should_run FROM delivery_history_fixture_execution)
       AND (SELECT count(*) FROM delivery_fixture_riders) < 10 THEN
        RAISE EXCEPTION 'Delivery history fixture rejected: fewer than 10 approved test riders are available.';
    END IF;
END $$;

CREATE TEMP TABLE delivery_fixture_locations ON COMMIT DROP AS
SELECT
    row_number() OVER (ORDER BY l."NormalizedAddress", l."Id")::integer AS location_no,
    l."Id" AS location_id,
    l."AddressLine" AS address_line,
    l."City" AS city,
    l."State" AS state,
    l."CountryCode" AS country_code,
    ST_Y(l."Coordinates"::geometry)::numeric(9, 6) AS latitude,
    ST_X(l."Coordinates"::geometry)::numeric(9, 6) AS longitude,
    z."Id" AS zone_id,
    z."Code" AS zone_code
FROM locations."Locations" l
JOIN LATERAL (
    SELECT dz."Id", dz."Code"
    FROM deliveries."DeliveryZones" dz
    WHERE dz."CountryCode" = 'NG'
      AND dz."IsActive" = TRUE
      AND dz."IsDeleted" = FALSE
      AND ST_Covers(dz."Boundary", l."Coordinates"::geometry)
    ORDER BY dz."Code"
    LIMIT 1
) z ON TRUE
WHERE l."CountryCode" = 'NG'
  AND l."ResolutionStatus" = 'Resolved'
  AND l."IsDeleted" = FALSE
ORDER BY l."NormalizedAddress", l."Id"
LIMIT 40;

DO $$
BEGIN
    IF (SELECT should_run FROM delivery_history_fixture_execution)
       AND (SELECT count(*) FROM delivery_fixture_locations) < 12 THEN
        RAISE EXCEPTION 'Delivery history fixture rejected: fewer than 12 resolved Enugu seed locations are available.';
    END IF;
END $$;

CREATE TEMP TABLE delivery_fixture_source ON COMMIT DROP AS
WITH fixture_context AS (
    SELECT
        s."Id" AS sender_id,
        c."Id" AS currency_id,
        p."Id" AS pricing_policy_id,
        p."Version" AS pricing_policy_version,
        (SELECT count(*)::integer FROM delivery_fixture_locations) AS location_count
    FROM authentication."Users" u
    JOIN stakeholders."Stakeholders" s ON s."AppUserId" = u."Id" AND s."IsDeleted" = FALSE
    CROSS JOIN payments."Currencies" c
    CROSS JOIN deliveries."DeliveryPricingPolicies" p
    WHERE u."NormalizedEmail" = 'PIKLYTECH+DELIVERYHISTORY@GMAIL.COM'
      AND u."IsDeleted" = FALSE
      AND c."CurrencyCode" = 'NGN' AND c."IsActive" = TRUE AND c."IsDeleted" = FALSE
      AND p."Version" = 'ENU-V1' AND p."IsDeleted" = FALSE
      AND (SELECT should_run FROM delivery_history_fixture_execution)
),
completed_rows AS (
    SELECT
        r.rider_no,
        r.rider_id,
        r.vehicle_category_id,
        r.vehicle_category_code,
        r.vehicle_category_name,
        series.delivery_no,
        row_number() OVER (ORDER BY series.delivery_no, r.rider_no)::integer AS sequence_no,
        7 AS final_status,
        format('PKL-DEMO-R%s-D%s', lpad(r.rider_no::text, 2, '0'), lpad(series.delivery_no::text, 2, '0')) AS tracking_id
    FROM delivery_fixture_riders r
    CROSS JOIN generate_series(1, 5) AS series(delivery_no)
),
additional_completed_rows AS (
    SELECT
        r.rider_no,
        r.rider_id,
        r.vehicle_category_id,
        r.vehicle_category_code,
        r.vehicle_category_name,
        100 + series.extra_no AS delivery_no,
        (SELECT count(*)::integer * 5 FROM delivery_fixture_riders) + series.extra_no AS sequence_no,
        7 AS final_status,
        format('PKL-DEMO-EXTRA-%s', lpad(series.extra_no::text, 2, '0')) AS tracking_id
    FROM generate_series(1, 20) AS series(extra_no)
    JOIN delivery_fixture_riders r
      ON r.rider_no = 1 + ((series.extra_no - 1) % (SELECT count(*)::integer FROM delivery_fixture_riders))
),
assigned_rows AS (
    SELECT
        r.rider_no,
        r.rider_id,
        r.vehicle_category_id,
        r.vehicle_category_code,
        r.vehicle_category_name,
        6 AS delivery_no,
        1000 + row_number() OVER (ORDER BY r.rider_no)::integer AS sequence_no,
        4 AS final_status,
        format('PKL-DEMO-PEND-%s', lpad(row_number() OVER (ORDER BY r.rider_no)::text, 2, '0')) AS tracking_id
    FROM delivery_fixture_riders r
    ORDER BY r.rider_no
    LIMIT 4
),
all_rows AS (
    SELECT * FROM completed_rows
    UNION ALL
    SELECT * FROM additional_completed_rows
    UNION ALL
    SELECT * FROM assigned_rows
),
timed_rows AS (
    SELECT
        a.*,
        CASE
            WHEN a.final_status = 7
                THEN NOW() - INTERVAL '14 days' + a.sequence_no * INTERVAL '3 hours'
            ELSE NOW() - (a.sequence_no - 999) * INTERVAL '45 minutes'
        END AS created_at_utc
    FROM all_rows a
)
SELECT
    gen_random_uuid() AS quote_id,
    gen_random_uuid() AS delivery_id,
    t.*,
    ctx.sender_id,
    ctx.currency_id,
    ctx.pricing_policy_id,
    ctx.pricing_policy_version,
    pickup.location_id AS pickup_location_id,
    pickup.address_line AS pickup_address_line,
    pickup.city AS pickup_city,
    pickup.state AS pickup_state,
    pickup.country_code AS pickup_country_code,
    pickup.latitude AS pickup_latitude,
    pickup.longitude AS pickup_longitude,
    pickup.zone_id AS origin_zone_id,
    pickup.zone_code AS origin_zone_code,
    dropoff.location_id AS dropoff_location_id,
    dropoff.address_line AS dropoff_address_line,
    dropoff.city AS dropoff_city,
    dropoff.state AS dropoff_state,
    dropoff.country_code AS dropoff_country_code,
    dropoff.latitude AS dropoff_latitude,
    dropoff.longitude AS dropoff_longitude,
    dropoff.zone_id AS destination_zone_id,
    dropoff.zone_code AS destination_zone_code,
    (1.0 + (t.sequence_no % 7) * 0.35)::numeric(12, 3) AS weight_kg,
    1200.00::numeric(18, 2) AS base_fee,
    (500 + (t.sequence_no % 5) * 250)::numeric(18, 2) AS distance_charge,
    (100 + (t.sequence_no % 3) * 50)::numeric(18, 2) AS weight_charge,
    CASE t.vehicle_category_code
        WHEN 'CAR' THEN 1500.00::numeric(18, 2)
        WHEN 'VAN' THEN 3500.00::numeric(18, 2)
        ELSE 0.00::numeric(18, 2)
    END AS vehicle_charge,
    CASE WHEN pickup.zone_id = dropoff.zone_id THEN 0.00 ELSE 200.00 END::numeric(18, 2) AS zone_charge
FROM timed_rows t
CROSS JOIN fixture_context ctx
JOIN delivery_fixture_locations pickup
  ON pickup.location_no = 1 + ((t.sequence_no - 1) % ctx.location_count)
JOIN delivery_fixture_locations dropoff
  ON dropoff.location_no = 1 + ((t.sequence_no + 10) % ctx.location_count)
WHERE NOT EXISTS (
    SELECT 1 FROM deliveries."Deliveries" d WHERE d."TrackingId" = t.tracking_id);

INSERT INTO deliveries."DeliveryQuotes"
    ("Id", "SenderId", "PickupLocationId", "DropOffLocationId",
     "PickupAddressLine", "PickupAdditionalInformation", "PickupCity", "PickupState", "PickupCountryCode",
     "PickupLatitude", "PickupLongitude", "DropOffAddressLine", "DropOffAdditionalInformation", "DropOffCity",
     "DropOffState", "DropOffCountryCode", "DropOffLatitude", "DropOffLongitude", "DeliveryType", "OrderType",
     "ScheduledPickupAtUtc", "VehicleCategoryId", "VehicleCategoryCode", "VehicleCategoryName", "TotalWeightKg",
     "OriginZoneId", "OriginZoneCode", "DestinationZoneId", "DestinationZoneCode", "PricingPolicyId",
     "PricingPolicyVersion", "BaseFee", "DistanceCharge", "WeightCharge", "VehicleCharge", "ZoneCharge",
     "TotalAmount", "CurrencyId", "IssuedAtUtc", "ExpiresAtUtc", "ConsumptionStatus", "ConsumedByDeliveryId",
     "ConsumedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy", "IsDeleted")
SELECT
    s.quote_id, s.sender_id, s.pickup_location_id, s.dropoff_location_id,
    s.pickup_address_line, 'Ask for the dispatch desk', s.pickup_city, s.pickup_state, s.pickup_country_code,
    s.pickup_latitude, s.pickup_longitude, s.dropoff_address_line, NULL, s.dropoff_city, s.dropoff_state,
    s.dropoff_country_code, s.dropoff_latitude, s.dropoff_longitude, 1, 2, NULL, s.vehicle_category_id,
    s.vehicle_category_code, s.vehicle_category_name, s.weight_kg, s.origin_zone_id, s.origin_zone_code,
    s.destination_zone_id, s.destination_zone_code, s.pricing_policy_id, s.pricing_policy_version,
    s.base_fee, s.distance_charge, s.weight_charge, s.vehicle_charge, s.zone_charge,
    s.base_fee + s.distance_charge + s.weight_charge + s.vehicle_charge + s.zone_charge,
    s.currency_id, s.created_at_utc - INTERVAL '20 minutes', s.created_at_utc + INTERVAL '10 minutes',
    1, NULL, NULL, s.created_at_utc - INTERVAL '20 minutes', 'system:delivery-history-fixture',
    s.created_at_utc - INTERVAL '20 minutes', 'system:delivery-history-fixture', FALSE
FROM delivery_fixture_source s;

INSERT INTO deliveries."DeliveryQuoteItems"
    ("Id", "DeliveryQuoteId", "Description", "Quantity", "WeightKg", "CreatedAtUtc", "CreatedBy",
     "UpdatedAtUtc", "UpdatedBy", "IsDeleted")
SELECT
    gen_random_uuid(), s.quote_id,
    CASE s.sequence_no % 5
        WHEN 0 THEN 'Packaged groceries'
        WHEN 1 THEN 'Small electronics parcel'
        WHEN 2 THEN 'Office documents'
        WHEN 3 THEN 'Clothing package'
        ELSE 'Household supplies'
    END,
    1, s.weight_kg, s.created_at_utc - INTERVAL '20 minutes', 'system:delivery-history-fixture',
    s.created_at_utc - INTERVAL '20 minutes', 'system:delivery-history-fixture', FALSE
FROM delivery_fixture_source s;

INSERT INTO deliveries."Deliveries"
    ("Id", "SenderId", "TrackingId", "DeliveryType", "OrderType", "Status", "PaymentStatus",
     "PickupAddressLine", "PickupAdditionalInformation", "PickupCity", "PickupState", "PickupCountryCode",
     "PickupLatitude", "PickupLongitude", "DropOffAddressLine", "DropOffAdditionalInformation", "DropOffCity",
     "DropOffState", "DropOffCountryCode", "DropOffLatitude", "DropOffLongitude", "RecipientName",
     "RecipientPhoneNumber", "ChargeAmount", "CurrencyId", "ScheduledPickupAtUtc", "DispatchEligibleAtUtc",
     "RiderId", "PaymentTransactionId", "PaymentSatisfiedAtUtc", "CashCollectionId", "ProofOfDeliveryId",
     "DeliveredAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy", "IsDeleted", "DeliveryQuoteId",
     "VehicleCategoryId")
SELECT
    s.delivery_id, s.sender_id, s.tracking_id, 1, 2, s.final_status,
    CASE WHEN s.final_status = 7 THEN 3 ELSE 1 END,
    s.pickup_address_line, 'Ask for the dispatch desk', s.pickup_city, s.pickup_state, s.pickup_country_code,
    s.pickup_latitude, s.pickup_longitude, s.dropoff_address_line, NULL, s.dropoff_city, s.dropoff_state,
    s.dropoff_country_code, s.dropoff_latitude, s.dropoff_longitude,
    'Demo Recipient ' || ((s.sequence_no % 12) + 1), '+234800000' || lpad((s.sequence_no % 10000)::text, 4, '0'),
    s.base_fee + s.distance_charge + s.weight_charge + s.vehicle_charge + s.zone_charge, s.currency_id,
    NULL, NULL, s.rider_id, NULL,
    CASE WHEN s.final_status = 7 THEN s.created_at_utc + INTERVAL '1 hour 5 minutes' ELSE NULL END,
    CASE WHEN s.final_status = 7 THEN gen_random_uuid() ELSE NULL END,
    CASE WHEN s.final_status = 7 THEN gen_random_uuid() ELSE NULL END,
    CASE WHEN s.final_status = 7 THEN s.created_at_utc + INTERVAL '1 hour 5 minutes' ELSE NULL END,
    s.created_at_utc, 'system:delivery-history-fixture',
    CASE WHEN s.final_status = 7 THEN s.created_at_utc + INTERVAL '1 hour 5 minutes' ELSE s.created_at_utc + INTERVAL '20 minutes' END,
    'system:delivery-history-fixture', FALSE, s.quote_id, s.vehicle_category_id
FROM delivery_fixture_source s;

INSERT INTO deliveries."DeliveryItems"
    ("Id", "DeliveryId", "Description", "Quantity", "WeightKg", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc",
     "UpdatedBy", "IsDeleted")
SELECT
    gen_random_uuid(), s.delivery_id,
    CASE s.sequence_no % 5
        WHEN 0 THEN 'Packaged groceries'
        WHEN 1 THEN 'Small electronics parcel'
        WHEN 2 THEN 'Office documents'
        WHEN 3 THEN 'Clothing package'
        ELSE 'Household supplies'
    END,
    1, s.weight_kg, s.created_at_utc, 'system:delivery-history-fixture', s.created_at_utc,
    'system:delivery-history-fixture', FALSE
FROM delivery_fixture_source s;

INSERT INTO deliveries."DeliveryStatusHistory"
    ("Id", "DeliveryId", "PreviousStatus", "NewStatus", "Reason", "OccurredAtUtc", "Actor", "Source",
     "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy", "IsDeleted")
SELECT
    gen_random_uuid(), s.delivery_id, step.previous_status, step.new_status, step.reason,
    s.created_at_utc + step.elapsed, 'system:delivery-history-fixture', 'delivery_history_fixture',
    s.created_at_utc + step.elapsed, 'system:delivery-history-fixture', s.created_at_utc + step.elapsed,
    'system:delivery-history-fixture', FALSE
FROM delivery_fixture_source s
CROSS JOIN LATERAL (VALUES
    (NULL::integer, 3, 'Delivery created.', INTERVAL '0 minutes', 3),
    (3, 4, 'Rider assigned.', INTERVAL '20 minutes', 4),
    (4, 5, 'Parcels picked up.', INTERVAL '35 minutes', 5),
    (5, 6, 'Delivery in transit.', INTERVAL '45 minutes', 6),
    (6, 7, 'Delivery completed.', INTERVAL '1 hour 5 minutes', 7)
) AS step(previous_status, new_status, reason, elapsed, represented_status)
WHERE step.represented_status <= s.final_status;

UPDATE deliveries."DeliveryQuotes" q
SET
    "ConsumptionStatus" = 2,
    "ConsumedByDeliveryId" = s.delivery_id,
    "ConsumedAtUtc" = s.created_at_utc,
    "UpdatedAtUtc" = s.created_at_utc,
    "UpdatedBy" = 'system:delivery-history-fixture'
FROM delivery_fixture_source s
WHERE q."Id" = s.quote_id;
