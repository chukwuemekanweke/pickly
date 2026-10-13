using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SignUp;

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
            nameof(IdentityErrorDescriber.DuplicateEmail) => nameof(SignUpCommand.Email),
            nameof(IdentityErrorDescriber.DuplicateUserName) => nameof(SignUpCommand.Email),
            nameof(IdentityErrorDescriber.InvalidEmail) => nameof(SignUpCommand.Email),
            nameof(IdentityErrorDescriber.InvalidUserName) => nameof(SignUpCommand.Email),
            nameof(IdentityErrorDescriber.PasswordMismatch) => nameof(SignUpCommand.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresDigit) => nameof(SignUpCommand.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresLower) => nameof(SignUpCommand.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric) => nameof(SignUpCommand.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresUpper) => nameof(SignUpCommand.Password),
            nameof(IdentityErrorDescriber.PasswordTooShort) => nameof(SignUpCommand.Password),
            nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars) => nameof(SignUpCommand.Password),
            _ => string.Empty
        };
}
