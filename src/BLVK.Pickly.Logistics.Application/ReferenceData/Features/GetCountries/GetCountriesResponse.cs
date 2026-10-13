namespace BLVK.Pickly.Logistics.Application.ReferenceData.Features.GetCountries;

public sealed record GetCountriesResponse(
    Guid CountryId,
    string Name,
    string ShortCode,
    string? CallingCode,
    string FlagUrl);
