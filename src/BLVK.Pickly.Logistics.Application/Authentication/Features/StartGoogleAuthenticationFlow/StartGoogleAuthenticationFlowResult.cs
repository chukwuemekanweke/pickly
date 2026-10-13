namespace BLVK.Pickly.Logistics.Application.Authentication.Features.StartGoogleAuthenticationFlow;

public sealed record StartGoogleAuthenticationFlowResult(
    string FlowToken,
    string Nonce,
    DateTimeOffset ExpiresAtUtc);
