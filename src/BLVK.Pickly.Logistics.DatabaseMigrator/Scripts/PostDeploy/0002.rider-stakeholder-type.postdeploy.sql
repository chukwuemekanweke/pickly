/*
    Seeds the "rider" stakeholder type for every tenant.

    Rider APIs and matching only accept stakeholders whose StakeholderType.Key is 'rider'
    (RiderActorResolver, RiderMatchingPersistence). The type is reference data like 'customer';
    assigning it to a stakeholder is an explicit operational step (see 0007.test-rider-accounts).

    Rerun-safe: the DatabaseMigrator runs every PostDeploy script on every deployment, so the insert is
    keyed by the filtered unique index IX_StakeholderTypes_TenantId_Key and never updates existing rows.
*/

INSERT INTO stakeholders."StakeholderTypes" ("Id", "TenantId", "Name", "Key", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "IsDeleted")
SELECT gen_random_uuid(), t."Id", 'Rider', 'rider', NOW(), 'system', NOW(), FALSE
FROM stakeholders."Tenants" t
WHERE t."IsDeleted" = FALSE
ON CONFLICT ("TenantId", "Key") WHERE "IsDeleted" = FALSE DO NOTHING;
