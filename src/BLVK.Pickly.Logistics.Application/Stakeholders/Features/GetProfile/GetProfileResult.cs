namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.GetProfile;

public sealed record GetProfileResult(
    GetProfileStatus Status,
    GetProfileResponse? Profile = null);

public enum GetProfileStatus
{
    Success = 1,
    NotAuthenticated = 2,
    StakeholderNotFound = 3
}
