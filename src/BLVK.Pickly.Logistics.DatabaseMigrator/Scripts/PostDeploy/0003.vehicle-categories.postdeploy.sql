/*
    Seeds deliveries."VehicleCategories" with the approved initial categories.

    Code is the stable natural key (filtered unique index IX_VehicleCategories_Code). Like the template seeder,
    this script is the declared state: every deployment inserts missing categories and realigns changed
    attributes. Quotes snapshot the category code and name, so realignment never changes historical quotes.
    Vehicle surcharges are pricing-policy rules (0006.enu-v1-pricing-policy.postdeploy.sql), not
    category attributes.

    RoutingProfile must be an official Mapbox Directions profile. Mapbox has no motorcycle, car or van
    specific profiles, so every category uses 'driving'; commercial vehicle names imply no road restrictions.
*/

WITH vehicle_values("Code", "Name", "Description", "MaximumWeightKg", "RoutingProfile", "IsActive") AS (VALUES
    ('MOTORCYCLE', 'Motorcycle', 'Motorcycle dispatch for parcels up to 10 kg.', 10.000, 'driving', TRUE),
    ('CAR', 'Car', 'Car dispatch for parcels up to 50 kg.', 50.000, 'driving', TRUE),
    ('VAN', 'Van', 'Van dispatch for parcels up to 300 kg.', 300.000, 'driving', TRUE)
)
INSERT INTO deliveries."VehicleCategories" ("Id", "Code", "Name", "Description", "MaximumWeightKg", "RoutingProfile", "IsActive", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "IsDeleted")
SELECT gen_random_uuid(), v."Code", v."Name", v."Description", v."MaximumWeightKg", v."RoutingProfile", v."IsActive", NOW(), 'system', NOW(), FALSE
FROM vehicle_values v
ON CONFLICT ("Code") WHERE "IsDeleted" = FALSE DO UPDATE
SET
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "MaximumWeightKg" = EXCLUDED."MaximumWeightKg",
    "RoutingProfile" = EXCLUDED."RoutingProfile",
    "IsActive" = EXCLUDED."IsActive",
    "UpdatedAtUtc" = NOW(),
    "UpdatedBy" = 'system'
WHERE deliveries."VehicleCategories"."Name" IS DISTINCT FROM EXCLUDED."Name"
   OR deliveries."VehicleCategories"."Description" IS DISTINCT FROM EXCLUDED."Description"
   OR deliveries."VehicleCategories"."MaximumWeightKg" IS DISTINCT FROM EXCLUDED."MaximumWeightKg"
   OR deliveries."VehicleCategories"."RoutingProfile" IS DISTINCT FROM EXCLUDED."RoutingProfile"
   OR deliveries."VehicleCategories"."IsActive" IS DISTINCT FROM EXCLUDED."IsActive";
