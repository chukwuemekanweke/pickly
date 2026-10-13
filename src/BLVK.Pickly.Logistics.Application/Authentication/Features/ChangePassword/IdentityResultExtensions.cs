using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.ChangePassword;

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
            nameof(IdentityErrorDescriber.PasswordMismatch) => nameof(ChangePasswordCommand.CurrentPassword),
            nameof(IdentityErrorDescriber.PasswordRequiresDigit) => nameof(ChangePasswordCommand.NewPassword),
            nameof(IdentityErrorDescriber.PasswordRequiresLower) => nameof(ChangePasswordCommand.NewPassword),
            nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric) => nameof(ChangePasswordCommand.NewPassword),
            nameof(IdentityErrorDescriber.PasswordRequiresUpper) => nameof(ChangePasswordCommand.NewPassword),
            nameof(IdentityErrorDescriber.PasswordTooShort) => nameof(ChangePasswordCommand.NewPassword),
            nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars) => nameof(ChangePasswordCommand.NewPassword),
            _ => nameof(ChangePasswordCommand.NewPassword)
        };
}
