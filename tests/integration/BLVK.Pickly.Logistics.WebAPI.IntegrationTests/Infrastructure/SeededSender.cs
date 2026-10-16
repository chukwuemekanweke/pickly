namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;

public sealed record SeededSender(
    string Email,
    Guid TenantId,
    Guid StakeholderId,
    Guid StakeholderTypeId,
    Guid? CreatedCountryId);
