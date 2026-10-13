using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RequestEmailConfirmationOtp;

public sealed record RequestEmailConfirmationOtpCommand(string Email, ActorContext ActorContext);
