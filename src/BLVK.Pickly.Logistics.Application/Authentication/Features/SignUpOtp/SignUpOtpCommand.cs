namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SignUpOtp;

using BLVK.Pickly.Logistics.Domain.Common.Auditing;

public sealed record SignUpOtpCommand(
    string Email,
    string Otp,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
