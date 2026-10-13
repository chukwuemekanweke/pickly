namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public interface IGoogleAuthenticationFlowService
{
    Task<GoogleAuthenticationFlow> StartAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<GoogleAuthenticationFlowResult> TakeAsync(string token, CancellationToken cancellationToken);
    Task RestoreAsync(GoogleAuthenticationFlow flow, CancellationToken cancellationToken);
    Task ConsumeAsync(GoogleAuthenticationFlow flow, CancellationToken cancellationToken);
}
