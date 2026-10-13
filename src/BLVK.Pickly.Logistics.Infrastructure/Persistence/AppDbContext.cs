using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : AppDbContextBase<AppDbContext>(options), IUnitOfWork
{
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfUnitOfWorkTransaction(await Database.BeginTransactionAsync(cancellationToken));
}
