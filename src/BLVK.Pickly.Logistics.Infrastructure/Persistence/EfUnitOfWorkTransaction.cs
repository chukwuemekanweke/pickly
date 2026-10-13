using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class EfUnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
