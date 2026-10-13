namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RequestPasswordReset;

using BLVK.Pickly.Logistics.Domain.Common.Auditing;

public sealed record RequestPasswordResetCommand(string Email, ActorContext ActorContext);
