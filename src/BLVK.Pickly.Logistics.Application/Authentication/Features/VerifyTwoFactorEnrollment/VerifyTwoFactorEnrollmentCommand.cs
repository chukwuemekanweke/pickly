using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.VerifyTwoFactorEnrollment;

public sealed record VerifyTwoFactorEnrollmentCommand(
    string Code,
    Guid CurrentSessionId,
    ActorContext ActorContext);
