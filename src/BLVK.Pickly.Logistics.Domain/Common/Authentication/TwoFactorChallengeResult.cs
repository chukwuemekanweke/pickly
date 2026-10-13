namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public sealed record TwoFactorChallengeResult(
    TwoFactorChallengeStatus Status,
    TwoFactorChallenge? Challenge = null);
