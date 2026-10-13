using BLVK.Pickly.Logistics.Domain.Common.Localization;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;

public sealed record GoogleSignUpRequest(
    string FlowToken,
    Guid CountryId,
    string FirstName,
    string LastName,
    string Language = SupportedLanguages.English);
