/*
    Rider readiness and matching diagnostics (read-only; safe in any environment).

      psql "$PICKLY_DB" -f rider-matching-diagnostics.sql
      psql "$PICKLY_DB" -v delivery_id=<delivery guid> -f rider-matching-diagnostics.sql

    Optional: -v location_freshness='2 minutes' (Riders:Matching:LocationFreshness).
    Section 1 lists every rider stakeholder and why it can or cannot receive offers. With delivery_id, section 2
    shows the delivery's matching state and section 3 evaluates every rider against that delivery using the
    same rules as RiderMatchingPersistence: tenant, verified rider, approved, exact vehicle, service zone for
    the quote's origin zone, Available, fresh position, no active delivery, no pending offer, within radius.
*/

\if :{?location_freshness}
\else
\set location_freshness '2 minutes'
\endif

\echo '== 1. Rider readiness'
SELECT u."Email" AS email,
       v."Code" AS vehicle,
       CASE p."VerificationStatus" WHEN 1 THEN 'Pending' WHEN 2 THEN 'Approved' WHEN 3 THEN 'Suspended' END AS approval,
       CASE p."AvailabilityStatus" WHEN 1 THEN 'Offline' WHEN 2 THEN 'Available' WHEN 3 THEN 'Paused' END AS availability,
       (SELECT string_agg(z."Code", ',' ORDER BY z."Code") FROM riders."RiderServiceZones" rz
        JOIN deliveries."DeliveryZones" z ON z."Id" = rz."DeliveryZoneId"
        WHERE rz."RiderProfileId" = p."Id" AND rz."IsDeleted" = FALSE) AS service_zones,
       date_trunc('second', NOW() - cl."CapturedAtUtc") AS location_age,
       CASE
           WHEN NOT s."IsVerified" THEN 'stakeholder not verified'
           WHEN p."Id" IS NULL THEN 'no rider profile'
           WHEN p."VerificationStatus" <> 2 THEN 'profile not approved'
           WHEN NOT v."IsActive" THEN 'vehicle category inactive'
           WHEN EXISTS (SELECT 1 FROM deliveries."Deliveries" d WHERE d."RiderId" = s."Id" AND d."Status" IN (4, 5, 6) AND d."IsDeleted" = FALSE) THEN 'busy: active delivery'
           WHEN p."AvailabilityStatus" <> 2 THEN 'availability not enabled'
           WHEN cl."Id" IS NULL THEN 'no position reported'
           WHEN cl."CapturedAtUtc" < NOW() - :'location_freshness'::interval THEN 'position stale'
           WHEN EXISTS (SELECT 1 FROM riders."RiderDeliveryOffers" o WHERE o."RiderProfileId" = p."Id" AND o."Status" = 1 AND o."IsDeleted" = FALSE) THEN 'holding a pending offer'
           ELSE 'READY'
       END AS readiness
FROM stakeholders."Stakeholders" s
JOIN stakeholders."StakeholderTypes" t ON t."Id" = s."StakeholderTypeId" AND t."Key" = 'rider'
JOIN authentication."Users" u ON u."Id" = s."AppUserId"
LEFT JOIN riders."RiderProfiles" p ON p."StakeholderId" = s."Id" AND p."IsDeleted" = FALSE
LEFT JOIN deliveries."VehicleCategories" v ON v."Id" = p."VehicleCategoryId"
LEFT JOIN riders."RiderCurrentLocations" cl ON cl."RiderProfileId" = p."Id" AND cl."IsDeleted" = FALSE
WHERE s."IsDeleted" = FALSE
ORDER BY u."Email";

\if :{?delivery_id}

\echo '== 2. Delivery matching state'
SELECT d."TrackingId",
       CASE d."Status" WHEN 1 THEN 'AwaitingPayment' WHEN 2 THEN 'AwaitingDispatch' WHEN 3 THEN 'PendingAssignment'
           WHEN 4 THEN 'RiderAssigned' WHEN 5 THEN 'PickedUp' WHEN 6 THEN 'InTransit' WHEN 7 THEN 'Delivered'
           WHEN 8 THEN 'DeliveryFailed' WHEN 9 THEN 'Cancelled' END AS status,
       CASE d."OrderType" WHEN 1 THEN 'PayBeforeDelivery' ELSE 'PayOnDelivery' END AS order_type,
       d."DispatchEligibleAtUtc", q."OriginZoneCode", q."VehicleCategoryCode",
       ms."Status" AS session_status, ms."AttemptCount", ms."SearchRadiusMeters", ms."NextAttemptAtUtc",
       (SELECT count(*) FROM riders."RiderDeliveryOffers" o WHERE o."DeliveryId" = d."Id") AS offers_made,
       (SELECT string_agg(CASE o."Status" WHEN 1 THEN 'Pending' WHEN 2 THEN 'Accepted' WHEN 3 THEN 'Rejected' WHEN 4 THEN 'Expired' ELSE 'Revoked' END, ',' ORDER BY o."OfferedAtUtc")
        FROM riders."RiderDeliveryOffers" o WHERE o."DeliveryId" = d."Id") AS offer_history
FROM deliveries."Deliveries" d
JOIN deliveries."DeliveryQuotes" q ON q."Id" = d."DeliveryQuoteId"
LEFT JOIN riders."DeliveryMatchingSessions" ms ON ms."DeliveryId" = d."Id" AND ms."IsDeleted" = FALSE AND ms."Status" = 1
WHERE d."Id" = :'delivery_id'::uuid;

\echo '== 3. Rider eligibility for this delivery (first failing rule)'
WITH delivery AS (
    SELECT d.*, q."OriginZoneId", sender."TenantId" AS sender_tenant_id,
           ST_SetSRID(ST_MakePoint(d."PickupLongitude"::float8, d."PickupLatitude"::float8), 4326) AS pickup,
           coalesce((SELECT ms."SearchRadiusMeters" FROM riders."DeliveryMatchingSessions" ms
                     WHERE ms."DeliveryId" = d."Id" AND ms."Status" = 1 AND ms."IsDeleted" = FALSE), 3000) AS radius_m
    FROM deliveries."Deliveries" d
    JOIN deliveries."DeliveryQuotes" q ON q."Id" = d."DeliveryQuoteId"
    JOIN stakeholders."Stakeholders" sender ON sender."Id" = d."SenderId"
    WHERE d."Id" = :'delivery_id'::uuid
)
SELECT * FROM (
SELECT u."Email" AS email,
       round(ST_Distance(cl."Position", dl.pickup::geography)::numeric) AS distance_m,
       CASE
           WHEN s."TenantId" <> dl.sender_tenant_id THEN 'different tenant'
           WHEN NOT s."IsVerified" THEN 'stakeholder not verified'
           WHEN p."VerificationStatus" <> 2 THEN 'profile not approved'
           WHEN p."VehicleCategoryId" <> dl."VehicleCategoryId" THEN 'wrong vehicle category'
           WHEN NOT EXISTS (SELECT 1 FROM riders."RiderServiceZones" rz WHERE rz."RiderProfileId" = p."Id" AND rz."DeliveryZoneId" = dl."OriginZoneId" AND rz."IsDeleted" = FALSE) THEN 'no service zone for pickup zone'
           WHEN p."AvailabilityStatus" <> 2 THEN 'availability not enabled'
           WHEN cl."Id" IS NULL THEN 'no position reported'
           WHEN cl."CapturedAtUtc" < NOW() - :'location_freshness'::interval THEN 'position stale'
           WHEN EXISTS (SELECT 1 FROM deliveries."Deliveries" a WHERE a."RiderId" = s."Id" AND a."Status" IN (4, 5, 6) AND a."IsDeleted" = FALSE) THEN 'busy: active delivery'
           WHEN EXISTS (SELECT 1 FROM riders."RiderDeliveryOffers" o WHERE o."RiderProfileId" = p."Id" AND o."Status" = 1 AND o."IsDeleted" = FALSE) THEN 'holding a pending offer'
           WHEN NOT ST_DWithin(cl."Position", dl.pickup::geography, dl.radius_m) THEN 'outside current search radius (' || dl.radius_m || ' m)'
           ELSE 'ELIGIBLE'
       END AS verdict
FROM delivery dl
CROSS JOIN riders."RiderProfiles" p
JOIN stakeholders."Stakeholders" s ON s."Id" = p."StakeholderId"
JOIN stakeholders."StakeholderTypes" t ON t."Id" = s."StakeholderTypeId" AND t."Key" = 'rider'
JOIN authentication."Users" u ON u."Id" = s."AppUserId"
LEFT JOIN riders."RiderCurrentLocations" cl ON cl."RiderProfileId" = p."Id" AND cl."IsDeleted" = FALSE
WHERE p."IsDeleted" = FALSE
) eligibility
ORDER BY verdict <> 'ELIGIBLE', distance_m NULLS LAST, email;

\endif
