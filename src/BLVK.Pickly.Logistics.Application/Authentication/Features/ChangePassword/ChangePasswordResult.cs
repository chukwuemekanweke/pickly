namespace BLVK.Pickly.Logistics.Application.Authentication.Features.ChangePassword;

public sealed record ChangePasswordResult(
    ChangePasswordStatus Status,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public enum ChangePasswordStatus
{
    Success = 1,
    NotAuthenticated = 2,
    UserNotFound = 3,
    IncorrectCurrentPassword = 4,
    ValidationFailed = 5
}
