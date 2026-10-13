using BLVK.Pickly.Logistics.Domain.Authentication.Entities;

namespace BLVK.Pickly.Logistics.Domain.Authentication.Persistence;

public interface IAppUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    void Remove(AppUser user);
}
