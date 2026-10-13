namespace BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;

public interface ILoginActivityReadModelRepository
{
    Task<LoginActivityHistoryCursorPage> GetByStakeholderAsync(
        LoginActivityHistoryCursorRequest request,
        CancellationToken cancellationToken);
}
