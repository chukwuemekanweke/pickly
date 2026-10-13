using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Domain.Authentication.Specifications;

public sealed class ActiveSessionsForStakeholderSpecification : Specification<AuthenticationSession>
{
    public ActiveSessionsForStakeholderSpecification(Guid stakeholderId, DateTimeOffset now)
    {
        Where(session => session.StakeholderId == stakeholderId && session.RevokedAtUtc == null && session.ExpiresAtUtc > now);
        AddInclude(session => session.FirstIpAddress);
        AddInclude(session => session.LastIpAddress);
        EnableTracking();
    }
}
