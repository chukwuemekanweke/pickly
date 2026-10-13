namespace BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;

public sealed record StakeholderReadModel(
    Guid StakeholderId,
    Guid AppUserId,
    string EmailAddress,
    Guid TenantId,
    Guid CountryId,
    Guid StakeholderTypeId,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    bool IsVerified)
{
    public string Theme { get; init; } = StakeholderThemes.Default;
    public string Language { get; init; } = Common.Localization.SupportedLanguages.Default;
}
