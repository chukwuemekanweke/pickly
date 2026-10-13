using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;

namespace BLVK.Pickly.Logistics.Application.Authentication;

public sealed class RegistrationCountryValidator(
    IRepository<Country> countryRepository,
    IIpGeolocationService ipGeolocationService)
{
    public async Task<RegistrationCountryValidationResult> ValidateAsync(
        Guid countryId,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var selectedCountry = await countryRepository.GetByIdAsync(countryId, cancellationToken);
        if (selectedCountry is null)
        {
            return RegistrationCountryValidationResult.CountryNotFound;
        }

        var geolocation = await ipGeolocationService.GetGeolocationAsync(ipAddress, cancellationToken);
        if (string.IsNullOrWhiteSpace(geolocation?.Country))
        {
            return RegistrationCountryValidationResult.Unavailable;
        }

        return string.Equals(geolocation.Country.Trim(), selectedCountry.Name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(geolocation.Country.Trim(), selectedCountry.ShortCode, StringComparison.OrdinalIgnoreCase)
                ? RegistrationCountryValidationResult.Match
                : RegistrationCountryValidationResult.Mismatch;
    }
}
