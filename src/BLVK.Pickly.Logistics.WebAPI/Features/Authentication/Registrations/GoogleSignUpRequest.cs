using BLVK.Pickly.Logistics.Domain.Common.Localization;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;

public sealed record GoogleSignUpRequest(
    string FlowToken,
    Guid? CountryId = null,
    string? FirstName = null,
    string? LastName = null,
    string? Language = SupportedLanguages.Default);
