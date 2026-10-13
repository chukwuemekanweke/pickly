using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.CompleteTwoFactorChallenge;

public sealed record CompleteTwoFactorChallengeCommand(
    string Challenge,
    TwoFactorVerificationMethod VerificationMethod,
    string Code);
