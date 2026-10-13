namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;

internal interface ISafeHavenClient
{
    Task<SafeHavenResponse<SafeHavenVirtualAccount>> CreateVirtualAccountAsync(
        SafeHavenCreateVirtualAccountRequest request,
        CancellationToken cancellationToken);

    Task<SafeHavenResponse<SafeHavenVirtualAccount>> GetVirtualAccountAsync(
        string virtualAccountId,
        CancellationToken cancellationToken);

    Task<SafeHavenResponse<SafeHavenIdentityVerification>> InitiateIdentityVerificationAsync(
        SafeHavenInitiateVerificationRequest request,
        CancellationToken cancellationToken);

    Task<SafeHavenResponse<SafeHavenIdentityVerification>> ValidateIdentityVerificationAsync(
        SafeHavenValidateVerificationRequest request,
        CancellationToken cancellationToken);

    Task<SafeHavenResponse<SafeHavenSubAccount>> CreateSubAccountAsync(
        SafeHavenCreateSubAccountRequest request,
        CancellationToken cancellationToken);

    Task<SafeHavenResponse<IReadOnlyList<SafeHavenAccountStatementEntry>>> GetAccountStatementAsync(
        SafeHavenAccountStatementRequest request,
        CancellationToken cancellationToken);
}
