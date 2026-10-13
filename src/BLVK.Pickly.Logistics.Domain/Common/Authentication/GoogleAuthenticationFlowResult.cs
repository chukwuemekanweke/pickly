namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public sealed record GoogleAuthenticationFlowResult(
    GoogleAuthenticationFlowStatus Status,
    GoogleAuthenticationFlow? Flow = null);
