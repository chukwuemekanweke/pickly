namespace BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;

public interface IActiveSessionReadModelRepository
{
    Task<IReadOnlyList<ActiveSessionReadModel>> GetActiveByStakeholderAsync(
        Guid stakeholderId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
