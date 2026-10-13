namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public interface IGoogleIdentityTokenService
{
    Task<GoogleIdentityTokenPayload?> ValidateAsync(
        string idToken,
        string expectedNonce,
        CancellationToken cancellationToken);
}
