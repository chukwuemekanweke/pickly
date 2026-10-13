namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.GetProfile;

public sealed record GetProfileResponse(
    Guid StakeholderId,
    string EmailAddress,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    bool IsVerified);
