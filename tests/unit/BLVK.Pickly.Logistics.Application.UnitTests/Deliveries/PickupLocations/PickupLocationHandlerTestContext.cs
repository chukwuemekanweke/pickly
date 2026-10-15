using BLVK.Pickly.Logistics.Application.Deliveries;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.DeletePickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.SetDefaultPickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

internal sealed class PickupLocationHandlerTestContext
{
    public PickupLocationHandlerTestContext()
    {
        Repository = new InMemoryRepository();
        UnitOfWork = new InMemoryUnitOfWork();
        ConsistencyLock = Substitute.For<IPickupLocationConsistencyLock>();
    }

    public InMemoryRepository Repository { get; }
    public InMemoryUnitOfWork UnitOfWork { get; }
    public IPickupLocationConsistencyLock ConsistencyLock { get; }

    public Guid TenantId { get; } = Guid.CreateVersion7();
    public Guid SenderId { get; } = Guid.CreateVersion7();
    public ActorContext Actor => new(SenderId, TenantId, "correlation", "flow");

    public CreatePickupLocationHandler CreateHandler() =>
        new(Repository, ConsistencyLock, UnitOfWork);

    public SetDefaultPickupLocationHandler SetDefaultHandler() =>
        new(Repository, ConsistencyLock, UnitOfWork);

    public DeletePickupLocationHandler DeleteHandler() =>
        new(Repository, ConsistencyLock, UnitOfWork, TimeProvider.System);

    public UpdatePickupLocationHandler UpdateHandler() => new(Repository, UnitOfWork);

    public static PickupLocationAddress Address(string line = "36 Garden Avenue") =>
        new(line, "Near the gate", "Enugu", "Enugu", "NG", 6.4474m, 7.5139m);

    internal sealed class InMemoryUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IUnitOfWorkTransaction>(new Transaction());

        private sealed class Transaction : IUnitOfWorkTransaction
        {
            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    internal sealed class InMemoryRepository : IRepository<SavedPickupLocation>
    {
        private readonly List<SavedPickupLocation> _locations = [];

        public IReadOnlyList<SavedPickupLocation> Locations => _locations.Where(location => !location.IsDeleted).ToArray();

        public Task<SavedPickupLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_locations.FirstOrDefault(location => location.Id == id && !location.IsDeleted));

        public Task<SavedPickupLocation?> FirstOrDefaultAsync(
            ISpecification<SavedPickupLocation> specification,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Apply(specification).FirstOrDefault());

        public Task<IReadOnlyList<SavedPickupLocation>> ListAsync(
            ISpecification<SavedPickupLocation> specification,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SavedPickupLocation>>(Apply(specification).ToArray());

        public Task<bool> AnyAsync(ISpecification<SavedPickupLocation> specification, CancellationToken cancellationToken = default) =>
            Task.FromResult(Apply(specification).Any());

        public Task<int> CountAsync(ISpecification<SavedPickupLocation> specification, CancellationToken cancellationToken = default) =>
            Task.FromResult(Apply(specification).Count());

        public Task AddAsync(SavedPickupLocation entity, CancellationToken cancellationToken = default)
        {
            if (entity.CreatedAtUtc == default)
            {
                entity.SetCreatedAudit(DateTimeOffset.UtcNow.AddTicks(_locations.Count), "test");
            }

            _locations.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(SavedPickupLocation entity)
        {
        }

        public void Remove(SavedPickupLocation entity) =>
            entity.SetDeletedAudit(DateTimeOffset.UtcNow, "test");

        private IEnumerable<SavedPickupLocation> Apply(ISpecification<SavedPickupLocation> specification)
        {
            IEnumerable<SavedPickupLocation> query = _locations.Where(location => !location.IsDeleted);
            if (specification.Criteria is not null)
            {
                query = query.Where(specification.Criteria.Compile());
            }

            IOrderedEnumerable<SavedPickupLocation>? ordered = null;
            if (specification.OrderBy is not null)
            {
                ordered = query.OrderBy(specification.OrderBy.Compile());
            }
            else if (specification.OrderByDescending is not null)
            {
                ordered = query.OrderByDescending(specification.OrderByDescending.Compile());
            }

            if (ordered is not null && specification.ThenBy is not null)
            {
                ordered = ordered.ThenBy(specification.ThenBy.Compile());
            }
            else if (ordered is not null && specification.ThenByDescending is not null)
            {
                ordered = ordered.ThenByDescending(specification.ThenByDescending.Compile());
            }

            query = ordered ?? query;
            if (specification.Skip.HasValue)
            {
                query = query.Skip(specification.Skip.Value);
            }

            if (specification.Take.HasValue)
            {
                query = query.Take(specification.Take.Value);
            }

            return query;
        }
    }
}
