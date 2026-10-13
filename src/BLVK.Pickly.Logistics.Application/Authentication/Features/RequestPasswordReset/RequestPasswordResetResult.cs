namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RequestPasswordReset;

public sealed record RequestPasswordResetResult(RequestPasswordResetStatus Status);

public enum RequestPasswordResetStatus
{
    Success = 1,
    UserNotFound = 2
}
