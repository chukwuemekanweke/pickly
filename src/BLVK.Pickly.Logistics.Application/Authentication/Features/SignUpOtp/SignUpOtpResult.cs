using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SignUpOtp;

public sealed record SignUpOtpResult(SignUpOtpStatus Status, AuthenticationTokens? Tokens = null);

public enum SignUpOtpStatus
{
    Success = 1,
    InvalidOtp = 2,
    AlreadyVerified = 3,
    AccountUnavailable = 4
}
