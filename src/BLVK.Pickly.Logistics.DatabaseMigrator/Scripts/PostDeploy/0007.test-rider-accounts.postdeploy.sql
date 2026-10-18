/*
    MVP test rider accounts for Enugu (dummy data - remove before real launch).

    Creates 18 ready-to-use rider accounts, piklytech+rider1@gmail.com .. piklytech+rider18@gmail.com, in every
    environment. Each can sign in immediately through POST /api/v1/authentication/sessions with the shared test
    password held by the product owner (the account is created email-confirmed, so no OTP is needed).

    Every row this script creates or changes is tagged CreatedBy/UpdatedBy = 'system:test-rider-fixture';
    Diagnostics/remove-test-rider-accounts.sql soft-deletes them all.

    Phase 6 has no rider onboarding API, so this mirrors what signup plus the domain would produce:
      AppUser          -> AppUser.Create(email), MarkEmailVerified(), ASP.NET Identity V3 hash of the default password
                          (PBKDF2-HMAC-SHA512, 100,000 iterations - the framework default the app verifies)
      Stakeholder      -> default tenant, country NG, StakeholderType 'rider' (0002), MarkVerified()
      RiderProfile     -> RiderProfile.Create (Offline, Pending), then Approve() unless the fixture is 'unapproved';
                          a vehicle change mirrors ChangeVehicleCategory() (forces Offline)
      RiderServiceZone -> exactly the fixture's zones (missing rows inserted, extra rows soft-deleted)
    Live presence is never seeded: positions expire after Riders:Matching:LocationFreshness, so riders report location
    and enable availability through PUT /api/v1/riders/me/location and /availability.

    Runs on every deployment and is idempotent:
      - accounts are created only when no user with that email exists; an existing account's password, lockout and
        2FA state are never reset;
      - profile, approval and service zones converge to the roster, then the script is a no-op;
      - it never fails a deployment over fixture data. It skips accounts with sender quotes/deliveries, vehicle
        changes while the rider has an active delivery, and an 'unapproved' fixture that is already Approved
        (there is no domain transition back to Pending).
*/

SELECT pg_advisory_xact_lock(hashtext('pickly.seed.test-rider-accounts'));

/* start_feature_id: OSM reference of the seeded location (0005) whose coordinates the rider reports. */
CREATE TEMP TABLE test_rider_roster ON COMMIT DROP AS
SELECT 'piklytech+rider' || v.fixture_no || '@gmail.com' AS email, v.*
FROM (VALUES
    (1,  'eligible',             'MOTORCYCLE', 'Approved', ARRAY['ENU-IND'], 'way/1421633152'),
    (2,  'eligible',             'MOTORCYCLE', 'Approved', ARRAY['ENU-OGU'], 'way/617584126'),
    (3,  'eligible',             'MOTORCYCLE', 'Approved', ARRAY['ENU-NHV'], 'way/792054618'),
    (4,  'eligible',             'MOTORCYCLE', 'Approved', ARRAY['ENU-GRA'], 'way/518124114'),
    (5,  'eligible',             'MOTORCYCLE', 'Approved', ARRAY['ENU-TEK'], 'way/791453174'),
    (6,  'eligible',             'MOTORCYCLE', 'Approved', ARRAY['ENU-ABK'], 'way/1433362624'),
    (7,  'eligible',             'MOTORCYCLE', 'Approved', ARRAY['ENU-THC'], 'node/8753679737'),
    (8,  'eligible',             'MOTORCYCLE', 'Approved', ARRAY['ENU-ACH'], 'node/13170601832'),
    (9,  'eligible',             'MOTORCYCLE', 'Approved', ARRAY['ENU-UWA'], 'way/1421633156'),
    (10, 'eligible',             'MOTORCYCLE', 'Approved', ARRAY['ENU-EME'], 'way/548692606'),
    (11, 'eligible-car',         'CAR',        'Approved', ARRAY['ENU-IND', 'ENU-OGU', 'ENU-NHV'], 'way/1062910020'),
    (12, 'eligible-car',         'CAR',        'Approved', ARRAY['ENU-GRA', 'ENU-OGU', 'ENU-TEK'], 'node/8753662624'),
    (13, 'eligible-van',         'VAN',        'Approved', ARRAY['ENU-IND', 'ENU-OGU', 'ENU-NHV', 'ENU-UWA'], 'way/1420853006'),
    (14, 'missing-service-zone', 'MOTORCYCLE', 'Approved', ARRAY['ENU-EME'], 'way/1421633152'),
    (15, 'unapproved',           'MOTORCYCLE', 'Pending',  ARRAY['ENU-IND'], 'way/1421633152'),
    (16, 'offline',              'MOTORCYCLE', 'Approved', ARRAY['ENU-IND'], 'way/1421633152'),
    (17, 'stale-location',       'MOTORCYCLE', 'Approved', ARRAY['ENU-IND'], 'way/1421633152'),
    (18, 'active-assignment',    'MOTORCYCLE', 'Approved', ARRAY['ENU-IND'], 'way/1420853006')
) AS v(fixture_no, role, vehicle_code, approval, zone_codes, start_feature_id);

DO $$
DECLARE
    fixture_actor CONSTANT text := 'system:test-rider-fixture';
    default_tenant_id CONSTANT uuid := '1203d9d1-2a6b-48ef-9cc1-e561a23aff72';
    /* ASP.NET Identity V3 hash of the shared test password. */
    default_password_hash CONSTANT text := 'AQAAAAIAAYagAAAAEMGNDYUsmtS/QXeD3mbI8tW6BymrqtIiJ5bR9i/qNaqHOLNXvIG9xIvEusB1gWNIDg==';
    nigeria_id uuid;
    rider_type_id uuid;
    fixture record;
    user_id uuid;
    stakeholder_id uuid;
    vehicle_id uuid;
    desired_status integer;
    profile riders."RiderProfiles"%ROWTYPE;
BEGIN
    SELECT c."Id" INTO nigeria_id FROM reference_data."Countries" c WHERE c."ShortCode" = 'NG' AND c."IsDeleted" = FALSE;
    SELECT t."Id" INTO rider_type_id FROM stakeholders."StakeholderTypes" t
    WHERE t."TenantId" = default_tenant_id AND t."Key" = 'rider' AND t."IsDeleted" = FALSE;

    /* Prerequisites come from 0001 and 0002; without them there is nothing to attach riders to. */
    IF nigeria_id IS NULL OR rider_type_id IS NULL THEN
        RETURN;
    END IF;

    FOR fixture IN SELECT * FROM test_rider_roster ORDER BY fixture_no LOOP
        SELECT v."Id" INTO vehicle_id FROM deliveries."VehicleCategories" v
        WHERE v."Code" = fixture.vehicle_code AND v."IsActive" = TRUE AND v."IsDeleted" = FALSE;
        CONTINUE WHEN vehicle_id IS NULL
            OR (SELECT count(*) FROM deliveries."DeliveryZones" z
                WHERE z."CountryCode" = 'NG' AND z."Code" = ANY (fixture.zone_codes) AND z."IsDeleted" = FALSE)
               <> cardinality(fixture.zone_codes);

        /* Identity user: created once, confirmed, default password. Existing accounts are left as they are. */
        INSERT INTO authentication."Users" ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy", "IsDeleted")
        SELECT gen_random_uuid(), fixture.email, upper(fixture.email), fixture.email, upper(fixture.email), TRUE, default_password_hash,
               upper(replace(gen_random_uuid()::text, '-', '')), gen_random_uuid()::text, FALSE, FALSE, TRUE, 0, NOW(), fixture_actor, NOW(), fixture_actor, FALSE
        WHERE NOT EXISTS (SELECT 1 FROM authentication."Users" u WHERE u."NormalizedEmail" = upper(fixture.email) AND u."IsDeleted" = FALSE);

        SELECT u."Id" INTO user_id FROM authentication."Users" u
        WHERE u."NormalizedEmail" = upper(fixture.email) AND u."IsDeleted" = FALSE;

        /* Stakeholder: created as a verified rider in the default tenant when the account has none. */
        INSERT INTO stakeholders."Stakeholders" ("Id", "AppUserId", "TenantId", "CountryId", "StakeholderTypeId", "FirstName", "LastName", "IsVerified", "Language", "Theme", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy", "IsDeleted")
        SELECT gen_random_uuid(), user_id, default_tenant_id, nigeria_id, rider_type_id, 'Test', 'Rider ' || fixture.fixture_no, TRUE, 'en', 'system', NOW(), fixture_actor, NOW(), fixture_actor, FALSE
        WHERE NOT EXISTS (SELECT 1 FROM stakeholders."Stakeholders" s WHERE s."AppUserId" = user_id AND s."IsDeleted" = FALSE);

        SELECT s."Id" INTO stakeholder_id FROM stakeholders."Stakeholders" s
        WHERE s."AppUserId" = user_id AND s."IsDeleted" = FALSE;

        /* Never turn an account that has acted as a sender into a rider. */
        CONTINUE WHEN EXISTS (SELECT 1 FROM deliveries."DeliveryQuotes" q WHERE q."SenderId" = stakeholder_id)
                   OR EXISTS (SELECT 1 FROM deliveries."Deliveries" d WHERE d."SenderId" = stakeholder_id);

        UPDATE stakeholders."Stakeholders" s
        SET "StakeholderTypeId" = t."Id", "IsVerified" = TRUE, "UpdatedAtUtc" = NOW(), "UpdatedBy" = fixture_actor
        FROM stakeholders."StakeholderTypes" t
        WHERE s."Id" = stakeholder_id AND t."TenantId" = s."TenantId" AND t."Key" = 'rider' AND t."IsDeleted" = FALSE
          AND (s."StakeholderTypeId" <> t."Id" OR s."IsVerified" = FALSE);

        desired_status := CASE fixture.approval WHEN 'Approved' THEN 2 ELSE 1 END;

        SELECT * INTO profile FROM riders."RiderProfiles"
        WHERE "StakeholderId" = stakeholder_id AND "IsDeleted" = FALSE;

        IF NOT FOUND THEN
            INSERT INTO riders."RiderProfiles" ("Id", "StakeholderId", "TenantId", "VehicleCategoryId", "AvailabilityStatus", "VerificationStatus", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "IsDeleted")
            SELECT gen_random_uuid(), s."Id", s."TenantId", vehicle_id, 1, desired_status, NOW(), fixture_actor, NOW(), FALSE
            FROM stakeholders."Stakeholders" s WHERE s."Id" = stakeholder_id
            ON CONFLICT ("StakeholderId") WHERE "IsDeleted" = FALSE DO NOTHING
            RETURNING * INTO profile;
            CONTINUE WHEN profile."Id" IS NULL;
        ELSE
            IF profile."VehicleCategoryId" <> vehicle_id
               AND NOT EXISTS (SELECT 1 FROM deliveries."Deliveries" d
                               WHERE d."RiderId" = stakeholder_id AND d."Status" IN (4, 5, 6) AND d."IsDeleted" = FALSE) THEN
                UPDATE riders."RiderProfiles"
                SET "VehicleCategoryId" = vehicle_id, "AvailabilityStatus" = 1, "UpdatedAtUtc" = NOW(), "UpdatedBy" = fixture_actor
                WHERE "Id" = profile."Id";
            END IF;

            IF desired_status = 2 AND profile."VerificationStatus" <> 2 THEN
                UPDATE riders."RiderProfiles"
                SET "VerificationStatus" = 2, "UpdatedAtUtc" = NOW(), "UpdatedBy" = fixture_actor
                WHERE "Id" = profile."Id";
            END IF;
        END IF;

        INSERT INTO riders."RiderServiceZones" ("Id", "RiderProfileId", "DeliveryZoneId", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "IsDeleted")
        SELECT gen_random_uuid(), profile."Id", z."Id", NOW(), fixture_actor, NOW(), FALSE
        FROM deliveries."DeliveryZones" z
        WHERE z."CountryCode" = 'NG' AND z."Code" = ANY (fixture.zone_codes) AND z."IsDeleted" = FALSE
        ON CONFLICT ("RiderProfileId", "DeliveryZoneId") WHERE "IsDeleted" = FALSE DO NOTHING;

        UPDATE riders."RiderServiceZones" rz
        SET "IsDeleted" = TRUE, "DeletedAtUtc" = NOW(), "DeletedBy" = fixture_actor, "UpdatedAtUtc" = NOW()
        FROM deliveries."DeliveryZones" z
        WHERE rz."RiderProfileId" = profile."Id" AND rz."IsDeleted" = FALSE
          AND z."Id" = rz."DeliveryZoneId" AND NOT (z."Code" = ANY (fixture.zone_codes));
    END LOOP;
END $$;
