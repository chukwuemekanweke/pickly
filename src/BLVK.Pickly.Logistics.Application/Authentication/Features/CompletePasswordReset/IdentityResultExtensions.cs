using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.CompletePasswordReset;

internal static class IdentityResultExtensions
{
    public static IReadOnlyDictionary<string, string[]> ToValidationDictionary(this IdentityResult result) =>
        result.Errors
            .GroupBy(GetPropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).Distinct().ToArray());

    private static string GetPropertyName(IdentityError error) =>
        error.Code switch
        {
            nameof(IdentityErrorDescriber.PasswordMismatch) => nameof(CompletePasswordResetCommand.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresDigit) => nameof(CompletePasswordResetCommand.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresLower) => nameof(CompletePasswordResetCommand.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric) => nameof(CompletePasswordResetCommand.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresUpper) => nameof(CompletePasswordResetCommand.Password),
            nameof(IdentityErrorDescriber.PasswordTooShort) => nameof(CompletePasswordResetCommand.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars) => nameof(CompletePasswordResetCommand.Password),
            _ => string.Empty
        };
}
