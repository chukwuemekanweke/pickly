using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignUp;

internal static class IdentityResultExtensions
{
    public static IReadOnlyDictionary<string, string[]> ToValidationDictionary(this IdentityResult result) =>
        result.Errors
            .GroupBy(GetPropertyName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray(), StringComparer.Ordinal);

    private static string GetPropertyName(IdentityError error) =>
        error.Code switch
        {
            nameof(IdentityErrorDescriber.DuplicateEmail) => nameof(GoogleSignUpCommand.FlowToken),
            nameof(IdentityErrorDescriber.DuplicateUserName) => nameof(GoogleSignUpCommand.FlowToken),
            nameof(IdentityErrorDescriber.InvalidEmail) => nameof(GoogleSignUpCommand.FlowToken),
            nameof(IdentityErrorDescriber.InvalidUserName) => nameof(GoogleSignUpCommand.FlowToken),
            nameof(IdentityErrorDescriber.LoginAlreadyAssociated) => nameof(GoogleSignUpCommand.FlowToken),
            _ => string.Empty
        };
}
