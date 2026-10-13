using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class AppUserRepository(AppDbContext dbContext) : IAppUserRepository
{
    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        dbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public void Remove(AppUser user) => dbContext.Users.Remove(user);
}
