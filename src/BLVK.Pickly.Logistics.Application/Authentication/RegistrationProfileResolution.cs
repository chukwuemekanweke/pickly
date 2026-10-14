namespace BLVK.Pickly.Logistics.Application.Authentication;

public sealed record RegistrationProfileResolution(
    RegistrationProfileResolutionStatus Status,
    RegistrationProfile? Profile = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
