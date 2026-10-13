namespace BLVK.Pickly.Logistics.Application.Authentication.Features.CompleteTwoFactorChallenge;

public sealed record CompleteTwoFactorChallengeResponse(
    string Outcome,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    string TokenType);
