using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class StakeholderReadModelRepository(AppReadDbContext dbContext) : IStakeholderReadModelRepository
{
    public async Task<StakeholderReadModel?> GetByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken = default) =>
        await dbContext.Stakeholders
            .Where(stakeholder => stakeholder.AppUserId == appUserId)
            .Select(CreateReadModel())
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<StakeholderReadModel?> GetByStakeholderIdAsync(Guid stakeholderId, CancellationToken cancellationToken = default)
        => await dbContext.Stakeholders
            .Where(stakeholder => stakeholder.Id == stakeholderId)
            .Select(CreateReadModel())
            .FirstOrDefaultAsync(cancellationToken);

    private static Expression<Func<Stakeholder, StakeholderReadModel>> CreateReadModel() =>
        stakeholder => new StakeholderReadModel(
            stakeholder.Id,
            stakeholder.AppUserId,
            stakeholder.AppUser.Email ?? string.Empty,
            stakeholder.TenantId,
            stakeholder.CountryId,
            stakeholder.StakeholderTypeId,
            stakeholder.FirstName,
            stakeholder.LastName,
            stakeholder.AvatarUrl,
            stakeholder.IsVerified)
        {
            Theme = stakeholder.Theme,
            Language = stakeholder.Language
        };
}
