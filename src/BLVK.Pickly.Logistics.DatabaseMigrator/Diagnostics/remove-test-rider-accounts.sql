/*
    Removes the MVP test rider accounts created by Scripts/PostDeploy/0007.test-rider-accounts.postdeploy.sql.

    FIRST delete 0007 from Scripts/PostDeploy and deploy. 0007 runs on every deployment and would recreate the
    accounts on the next deploy otherwise.

    Then run once per environment:
      psql "$PICKLY_DB" -v ON_ERROR_STOP=1 --single-transaction -f remove-test-rider-accounts.sql

    Affects only users created by 0007 (CreatedBy = 'system:test-rider-fixture') and their stakeholder, rider
    profile, service zones, current location, sessions and refresh tokens. Everything is soft-deleted, so
    historical deliveries that reference these riders stay intact; the soft-delete query filter hides the accounts
    from sign-in. Reference data (rider stakeholder type, vehicles, zones, locations, ENU-V1) is not touched.

    Refuses to run, changing nothing, while a test rider holds a pending offer or an active delivery: switch the
    rider offline (PUT /api/v1/riders/me/availability) or finish the delivery first. Safe to rerun; a second run
    finds nothing left to remove.
*/

SELECT pg_advisory_xact_lock(hashtext('pickly.seed.test-rider-accounts'));

CREATE TEMP TABLE test_rider_account ON COMMIT DROP AS
SELECT u."Id" AS user_id, s."Id" AS stakeholder_id, p."Id" AS profile_id
FROM authentication."Users" u
LEFT JOIN stakeholders."Stakeholders" s ON s."AppUserId" = u."Id" AND s."IsDeleted" = FALSE
LEFT JOIN riders."RiderProfiles" p ON p."StakeholderId" = s."Id" AND p."IsDeleted" = FALSE
WHERE u."CreatedBy" = 'system:test-rider-fixture' AND u."IsDeleted" = FALSE;

DO $$
DECLARE
    blocked text;
    removed integer;
BEGIN
    SELECT string_agg(DISTINCT u."Email", ', ') INTO blocked
    FROM test_rider_account a
    JOIN authentication."Users" u ON u."Id" = a.user_id
    WHERE EXISTS (SELECT 1 FROM deliveries."Deliveries" d
                  WHERE d."RiderId" = a.stakeholder_id AND d."Status" IN (4, 5, 6) AND d."IsDeleted" = FALSE)
       OR EXISTS (SELECT 1 FROM riders."RiderDeliveryOffers" o
                  WHERE o."RiderProfileId" = a.profile_id AND o."Status" = 1 AND o."IsDeleted" = FALSE);

    IF blocked IS NOT NULL THEN
        RAISE EXCEPTION 'Test riders still hold a pending offer or an active delivery: %. Nothing was removed.', blocked;
    END IF;

    UPDATE authentication."Sessions" x
    SET "RevokedAtUtc" = NOW(), "UpdatedAtUtc" = NOW(), "UpdatedBy" = 'system:test-rider-cleanup'
    FROM test_rider_account a
    WHERE x."StakeholderId" = a.stakeholder_id AND x."RevokedAtUtc" IS NULL;

    UPDATE authentication."RefreshTokens" x
    SET "RevokedAtUtc" = NOW(), "UpdatedAtUtc" = NOW(), "UpdatedBy" = 'system:test-rider-cleanup'
    FROM test_rider_account a
    WHERE x."AppUserId" = a.user_id AND x."RevokedAtUtc" IS NULL;

    UPDATE riders."RiderCurrentLocations" x
    SET "IsDeleted" = TRUE, "DeletedAtUtc" = NOW(), "DeletedBy" = 'system:test-rider-cleanup', "UpdatedAtUtc" = NOW()
    FROM test_rider_account a
    WHERE x."RiderProfileId" = a.profile_id AND x."IsDeleted" = FALSE;

    UPDATE riders."RiderServiceZones" x
    SET "IsDeleted" = TRUE, "DeletedAtUtc" = NOW(), "DeletedBy" = 'system:test-rider-cleanup', "UpdatedAtUtc" = NOW()
    FROM test_rider_account a
    WHERE x."RiderProfileId" = a.profile_id AND x."IsDeleted" = FALSE;

    UPDATE riders."RiderProfiles" x
    SET "AvailabilityStatus" = 1, "IsDeleted" = TRUE, "DeletedAtUtc" = NOW(), "DeletedBy" = 'system:test-rider-cleanup', "UpdatedAtUtc" = NOW()
    FROM test_rider_account a
    WHERE x."Id" = a.profile_id;

    UPDATE stakeholders."Stakeholders" x
    SET "IsDeleted" = TRUE, "DeletedAtUtc" = NOW(), "DeletedBy" = 'system:test-rider-cleanup', "UpdatedAtUtc" = NOW()
    FROM test_rider_account a
    WHERE x."Id" = a.stakeholder_id;

    /* Rotating the security stamp also invalidates anything issued against the old stamp. */
    UPDATE authentication."Users" x
    SET "IsDeleted" = TRUE, "DeletedAtUtc" = NOW(), "DeletedBy" = 'system:test-rider-cleanup', "UpdatedAtUtc" = NOW(),
        "SecurityStamp" = upper(replace(gen_random_uuid()::text, '-', ''))
    FROM test_rider_account a
    WHERE x."Id" = a.user_id;
    GET DIAGNOSTICS removed = ROW_COUNT;

    RAISE NOTICE 'Removed % test rider account(s).', removed;
END $$;
