using BLVK.Pickly.Logistics.Domain.Authentication.Entities;

namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public interface IAccessTokenService
{
    AccessToken Generate(AppUser user, Guid stakeholderId, Guid sessionId);
}
