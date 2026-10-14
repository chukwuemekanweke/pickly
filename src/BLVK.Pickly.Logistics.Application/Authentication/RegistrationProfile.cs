namespace BLVK.Pickly.Logistics.Application.Authentication;

public sealed record RegistrationProfile(
    Guid CountryId,
    string FirstName,
    string LastName,
    string Language);
