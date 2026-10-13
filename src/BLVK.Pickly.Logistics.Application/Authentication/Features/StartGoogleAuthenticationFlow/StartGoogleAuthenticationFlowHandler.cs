using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.StartGoogleAuthenticationFlow;

public sealed class StartGoogleAuthenticationFlowHandler(
    IGoogleAuthenticationFlowService googleAuthenticationFlowService)
{
    public async Task<StartGoogleAuthenticationFlowResult> HandleAsync(
        StartGoogleAuthenticationFlowCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = request.ActorContext.TenantId
            ?? throw new InvalidOperationException("Tenant id is required to start Google authentication.");
        var flow = await googleAuthenticationFlowService.StartAsync(tenantId, cancellationToken);

        return new StartGoogleAuthenticationFlowResult(flow.Token, flow.Nonce, flow.ExpiresAtUtc);
    }
}
