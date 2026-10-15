using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.DeletePickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDefaultPickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetPickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.ListPickupLocations;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.SetDefaultPickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.PickupLocations;

internal sealed class PickupLocationsControllerTestContext
{
    public PickupLocationsControllerTestContext()
    {
        Repository = Substitute.For<IRepository<SavedPickupLocation>>();
        ReadRepository = Substitute.For<ISavedPickupLocationReadModelRepository>();
        UnitOfWork = Substitute.For<IUnitOfWork>();
        Transaction = Substitute.For<IUnitOfWorkTransaction>();
        ConsistencyLock = Substitute.For<IPickupLocationConsistencyLock>();
        CurrentActor = Substitute.For<ICurrentActor>();

        CurrentActor.ActorId.Returns(SenderId.ToString());
        CurrentActor.TenantId.Returns(TenantId);
        CurrentActor.CorrelationId.Returns("correlation");
        CurrentActor.FlowId.Returns("flow");
        UnitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Transaction);
    }

    public Guid TenantId { get; } = Guid.CreateVersion7();
    public Guid SenderId { get; } = Guid.CreateVersion7();
    public IRepository<SavedPickupLocation> Repository { get; }
    public ISavedPickupLocationReadModelRepository ReadRepository { get; }
    public IUnitOfWork UnitOfWork { get; }
    public IUnitOfWorkTransaction Transaction { get; }
    public IPickupLocationConsistencyLock ConsistencyLock { get; }
    public ICurrentActor CurrentActor { get; }

    public PickupLocationsController CreateController() =>
        new(
            new CreatePickupLocationHandler(Repository, ConsistencyLock, UnitOfWork),
            new ListPickupLocationsHandler(ReadRepository),
            new GetDefaultPickupLocationHandler(ReadRepository),
            new GetPickupLocationHandler(ReadRepository),
            new UpdatePickupLocationHandler(Repository, UnitOfWork),
            new SetDefaultPickupLocationHandler(Repository, ConsistencyLock, UnitOfWork),
            new DeletePickupLocationHandler(Repository, ConsistencyLock, UnitOfWork, TimeProvider.System),
            new CreatePickupLocationRequestValidator(),
            new UpdatePickupLocationRequestValidator(),
            new ListPickupLocationsRequestValidator(),
            CurrentActor);

    public static CreatePickupLocationRequest CreateRequest(string label = "Home") =>
        new(label, Address());

    public static PickupLocationAddressRequest Address(string line = "36 Garden Avenue") =>
        new(line, null, "Enugu", "Enugu", "NG", 6.4474m, 7.5139m);

    public SavedPickupLocation Location(string label = "Home", bool isDefault = false) =>
        SavedPickupLocation.Create(
            SenderId,
            label,
            DeliveryAddress.Create(
                "36 Garden Avenue",
                null,
                "Enugu",
                "Enugu",
                "NG",
                6.4474m,
                7.5139m),
            isDefault);
}
