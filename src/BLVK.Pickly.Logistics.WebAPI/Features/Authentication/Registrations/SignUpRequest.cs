using BLVK.Pickly.Logistics.Domain.Common.Localization;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;

public sealed record SignUpRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    Guid? CountryId = null,
    string? FirstName = null,
    string? LastName = null,
    string? Language = SupportedLanguages.Default);
