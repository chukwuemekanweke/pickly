using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.DisableTwoFactor;

public sealed record DisableTwoFactorCommand(
    TwoFactorVerificationMethod VerificationMethod,
    string Code,
    Guid CurrentSessionId,
    ActorContext ActorContext);
