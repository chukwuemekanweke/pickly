namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.UpdatePreferences;

public enum UpdatePreferencesStatus
{
    Success = 1,
    NotAuthenticated = 2,
    StakeholderNotFound = 3,
    ValidationFailed = 4
}

public sealed record UpdatePreferencesResult(UpdatePreferencesStatus Status);
