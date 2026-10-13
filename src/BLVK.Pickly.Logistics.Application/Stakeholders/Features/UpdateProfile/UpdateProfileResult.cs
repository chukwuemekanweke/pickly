namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.UpdateProfile;

public sealed record UpdateProfileResult(
    UpdateProfileStatus Status,
    string? Error = null);

public enum UpdateProfileStatus
{
    Success = 1,
    NotAuthenticated = 2,
    StakeholderNotFound = 3,
    ValidationFailed = 4
}
