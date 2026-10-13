namespace BLVK.Pickly.Logistics.Application.Authentication.Features.LogoutSession;

public sealed record LogoutSessionResult(LogoutSessionStatus Status);

public enum LogoutSessionStatus
{
    Success = 1,
    InvalidToken = 2
}
