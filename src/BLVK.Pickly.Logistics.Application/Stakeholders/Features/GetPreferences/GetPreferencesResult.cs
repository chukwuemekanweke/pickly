namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.GetPreferences;

public enum GetPreferencesStatus
{
    Success = 1,
    NotAuthenticated = 2,
    StakeholderNotFound = 3
}

public sealed record GetPreferencesResult(
    GetPreferencesStatus Status,
    GetPreferencesResponse? Preferences = null);
