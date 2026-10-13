namespace BLVK.Pickly.Logistics.Application.Authentication.Features.CompletePasswordReset;

using BLVK.Pickly.Logistics.Domain.Common.Auditing;

public sealed record CompletePasswordResetCommand(
    string Email,
    string Otp,
    string Password,
    string ConfirmPassword,
    ActorContext ActorContext);
