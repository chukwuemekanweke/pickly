namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SignUpOtp;

public sealed record SignUpOtpResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    string TokenType);
