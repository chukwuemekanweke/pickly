using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Localization;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Specifications;

namespace BLVK.Pickly.Logistics.Application.Authentication;

public sealed class RegistrationProfileResolver(
    IRegistrationPolicyProvider registrationPolicyProvider,
    IRepository<Country> countryRepository,
    RegistrationCountryValidator countryValidator)
{
    private const int MaxNameLength = 100;
    private const string MvpCountryShortCode = "NG";

    public async Task<RegistrationProfileResolution> ResolveAsync(
        string? firstName,
        string? lastName,
        Guid? countryId,
        string? language,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var policy = await registrationPolicyProvider.GetAsync();
        var validationErrors = ValidateProfileDetails(firstName, lastName, countryId, language, policy);
        if (validationErrors.Count > 0)
        {
            return new RegistrationProfileResolution(
                RegistrationProfileResolutionStatus.ValidationFailed,
                ValidationErrors: validationErrors);
        }

        if (policy.IsMvp)
        {
            var nigeria = await countryRepository.FirstOrDefaultAsync(
                new CountryByShortCodeSpecification(MvpCountryShortCode),
                cancellationToken);
            if (nigeria is null)
            {
                throw new InvalidOperationException(
                    $"MVP registration requires country '{MvpCountryShortCode}' to exist in reference data.");
            }

            return Success(
                nigeria.Id,
                ResolveName(firstName),
                ResolveName(lastName),
                string.IsNullOrWhiteSpace(language) ? SupportedLanguages.Default : language);
        }

        var resolvedCountryId = countryId!.Value;
        var countryValidation = await countryValidator.ValidateAsync(
            resolvedCountryId,
            ipAddress,
            cancellationToken);

        if (countryValidation == RegistrationCountryValidationResult.CountryNotFound)
        {
            return new RegistrationProfileResolution(
                RegistrationProfileResolutionStatus.ValidationFailed,
                ValidationErrors: new Dictionary<string, string[]>
                {
                    [nameof(RegistrationProfile.CountryId)] = ["The selected country is invalid."]
                });
        }

        if (countryValidation == RegistrationCountryValidationResult.Mismatch)
        {
            return new RegistrationProfileResolution(RegistrationProfileResolutionStatus.CountryMismatch);
        }

        return Success(
            resolvedCountryId,
            firstName!.Trim(),
            lastName!.Trim(),
            language!.Trim());
    }

    private static Dictionary<string, string[]> ValidateProfileDetails(
        string? firstName,
        string? lastName,
        Guid? countryId,
        string? language,
        RegistrationPolicy policy)
    {
        var errors = new Dictionary<string, string[]>();

        ValidateName(firstName, nameof(RegistrationProfile.FirstName), policy.RequireProfileDetails, errors);
        ValidateName(lastName, nameof(RegistrationProfile.LastName), policy.RequireProfileDetails, errors);

        if (policy.RequireProfileDetails && (!countryId.HasValue || countryId.Value == Guid.Empty))
        {
            errors[nameof(RegistrationProfile.CountryId)] = ["Country is required."];
        }

        if (string.IsNullOrWhiteSpace(language))
        {
            if (policy.RequireProfileDetails)
            {
                errors[nameof(RegistrationProfile.Language)] = ["Language is required."];
            }
        }
        else if (!SupportedLanguages.IsSupported(language.Trim()))
        {
            errors[nameof(RegistrationProfile.Language)] = ["Language is not supported."];
        }

        return errors;
    }

    private static void ValidateName(
        string? value,
        string propertyName,
        bool isRequired,
        IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (isRequired)
            {
                errors[propertyName] = ["Name is required."];
            }

            return;
        }

        if (value.Trim().Length > MaxNameLength)
        {
            errors[propertyName] = [$"Name must not exceed {MaxNameLength} characters."];
        }
    }

    private static string ResolveName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? RegistrationNamePlaceholderGenerator.Generate() : value.Trim();

    private static RegistrationProfileResolution Success(
        Guid countryId,
        string firstName,
        string lastName,
        string language) =>
        new(
            RegistrationProfileResolutionStatus.Success,
            new RegistrationProfile(countryId, firstName, lastName, language));
}
