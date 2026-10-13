namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.GoogleFlows;

public sealed record GoogleAuthenticationFlowResponse(
    string FlowToken,
    string Nonce,
    DateTimeOffset ExpiresAtUtc);
