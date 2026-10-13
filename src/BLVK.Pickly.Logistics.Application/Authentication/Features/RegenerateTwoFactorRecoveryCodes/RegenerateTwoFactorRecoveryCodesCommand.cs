using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RegenerateTwoFactorRecoveryCodes;

public sealed record RegenerateTwoFactorRecoveryCodesCommand(
    TwoFactorVerificationMethod VerificationMethod,
    string Code,
    Guid CurrentSessionId,
    ActorContext ActorContext);
