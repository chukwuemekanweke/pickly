namespace BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;

public interface IStakeholderReadModelRepository
{
    Task<StakeholderReadModel?> GetByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken = default);
    Task<StakeholderReadModel?> GetByStakeholderIdAsync(Guid stakeholderId, CancellationToken cancellationToken = default);
}
