namespace BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignIn;

public sealed record GoogleSignInResponse(
    string Outcome,
    string? AccessToken = null,
    DateTimeOffset? ExpiresAtUtc = null,
    string? RefreshToken = null,
    DateTimeOffset? RefreshTokenExpiresAtUtc = null,
    string? TokenType = null,
    string? Challenge = null,
    DateTimeOffset? ChallengeExpiresAtUtc = null);
