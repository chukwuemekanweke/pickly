namespace BLVK.Pickly.Logistics.Contracts.Events;

public static class UserSignInFailureReasons
{
    public const string UserNotFound = "user_not_found";
    public const string InvalidCredentials = "invalid_credentials";
    public const string EmailNotVerified = "email_not_verified";
    public const string LockedOut = "locked_out";
}
