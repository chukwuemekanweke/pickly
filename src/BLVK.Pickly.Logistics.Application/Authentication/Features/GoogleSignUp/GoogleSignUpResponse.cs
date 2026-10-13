namespace BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignUp;

public sealed record GoogleSignUpResponse(
    string Outcome,
    string Email,
    string? AccessToken = null,
    DateTimeOffset? ExpiresAtUtc = null,
    string? RefreshToken = null,
    DateTimeOffset? RefreshTokenExpiresAtUtc = null,
    string? TokenType = null);
