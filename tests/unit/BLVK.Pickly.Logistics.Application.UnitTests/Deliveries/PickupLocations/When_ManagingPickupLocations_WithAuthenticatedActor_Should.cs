using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.DeletePickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.SetDefaultPickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed class When_ManagingPickupLocations_WithAuthenticatedActor_Should
{
    [Fact]
    public async Task MakeFirstLocationDefaultAndLeaveAdditionalLocationNonDefault()
    {
        var context = new PickupLocationHandlerTestContext();
        var handler = context.CreateHandler();

        var first = await handler.HandleAsync(
            new CreatePickupLocationCommand("Home", PickupLocationHandlerTestContext.Address(), false, context.Actor),
            CancellationToken.None);
        var second = await handler.HandleAsync(
            new CreatePickupLocationCommand("Office", PickupLocationHandlerTestContext.Address("Suite C9"), false, context.Actor),
            CancellationToken.None);

        first.Status.ShouldBe(CreatePickupLocationStatus.Success);
        first.Location!.IsDefault.ShouldBeTrue();
        second.Location!.IsDefault.ShouldBeFalse();
        context.Repository.Locations.Count(location => location.IsDefault).ShouldBe(1);
    }

    [Fact]
    public async Task ReplaceDefaultWhenSecondLocationExplicitlyRequestsIt()
    {
        var context = new PickupLocationHandlerTestContext();
        var handler = context.CreateHandler();

        var first = await handler.HandleAsync(
            new CreatePickupLocationCommand("Home", PickupLocationHandlerTestContext.Address(), false, context.Actor),
            CancellationToken.None);
        var second = await handler.HandleAsync(
            new CreatePickupLocationCommand("Office", PickupLocationHandlerTestContext.Address("Suite C9"), true, context.Actor),
            CancellationToken.None);

        context.Repository.Locations.Single(location => location.Id == first.Location!.Id).IsDefault.ShouldBeFalse();
        context.Repository.Locations.Single(location => location.Id == second.Location!.Id).IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task TreatSettingCurrentDefaultAsSuccessfulNoOp()
    {
        var context = new PickupLocationHandlerTestContext();
        var created = await context.CreateHandler().HandleAsync(
            new CreatePickupLocationCommand("Home", PickupLocationHandlerTestContext.Address(), false, context.Actor),
            CancellationToken.None);
        var saveCountBefore = context.UnitOfWork.SaveCount;

        var result = await context.SetDefaultHandler().HandleAsync(
            new SetDefaultPickupLocationCommand(created.Location!.Id, context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(SetDefaultPickupLocationStatus.Success);
        context.UnitOfWork.SaveCount.ShouldBe(saveCountBefore);
    }

    [Fact]
    public async Task KeepDefaultWhenDeletingNonDefaultLocation()
    {
        var context = new PickupLocationHandlerTestContext();
        var create = context.CreateHandler();
        var first = await create.HandleAsync(
            new CreatePickupLocationCommand("Home", PickupLocationHandlerTestContext.Address(), false, context.Actor),
            CancellationToken.None);
        var second = await create.HandleAsync(
            new CreatePickupLocationCommand("Office", PickupLocationHandlerTestContext.Address("Suite C9"), false, context.Actor),
            CancellationToken.None);

        var result = await context.DeleteHandler().HandleAsync(
            new DeletePickupLocationCommand(second.Location!.Id, context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(DeletePickupLocationStatus.Success);
        context.Repository.Locations.Single().Id.ShouldBe(first.Location!.Id);
        context.Repository.Locations.Single().IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task SelectOldestReplacementWhenDeletingDefault()
    {
        var context = new PickupLocationHandlerTestContext();
        var create = context.CreateHandler();
        var first = await create.HandleAsync(
            new CreatePickupLocationCommand("Home", PickupLocationHandlerTestContext.Address(), false, context.Actor),
            CancellationToken.None);
        var oldestRemaining = await create.HandleAsync(
            new CreatePickupLocationCommand("Office", PickupLocationHandlerTestContext.Address("Suite C9"), false, context.Actor),
            CancellationToken.None);
        await create.HandleAsync(
            new CreatePickupLocationCommand("Warehouse", PickupLocationHandlerTestContext.Address("Dock 2"), false, context.Actor),
            CancellationToken.None);

        await context.DeleteHandler().HandleAsync(
            new DeletePickupLocationCommand(first.Location!.Id, context.Actor),
            CancellationToken.None);

        context.Repository.Locations.Single(location => location.IsDefault).Id.ShouldBe(oldestRemaining.Location!.Id);
    }

    [Fact]
    public async Task LeaveNoDefaultWhenDeletingLastLocation()
    {
        var context = new PickupLocationHandlerTestContext();
        var created = await context.CreateHandler().HandleAsync(
            new CreatePickupLocationCommand("Home", PickupLocationHandlerTestContext.Address(), false, context.Actor),
            CancellationToken.None);

        await context.DeleteHandler().HandleAsync(
            new DeletePickupLocationCommand(created.Location!.Id, context.Actor),
            CancellationToken.None);

        context.Repository.Locations.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateDetailsWithoutChangingOwnershipOrDefault()
    {
        var context = new PickupLocationHandlerTestContext();
        var created = await context.CreateHandler().HandleAsync(
            new CreatePickupLocationCommand("Home", PickupLocationHandlerTestContext.Address(), false, context.Actor),
            CancellationToken.None);

        var result = await context.UpdateHandler().HandleAsync(
            new UpdatePickupLocationCommand(
                created.Location!.Id,
                "Office",
                PickupLocationHandlerTestContext.Address("Suite C9"),
                context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(UpdatePickupLocationStatus.Success);
        var updated = context.Repository.Locations.Single();
        updated.Label.ShouldBe("Office");
        updated.Address.AddressLine.ShouldBe("Suite C9");
        updated.SenderId.ShouldBe(context.SenderId);
        updated.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task HideLocationFromDifferentSender()
    {
        var context = new PickupLocationHandlerTestContext();
        var created = await context.CreateHandler().HandleAsync(
            new CreatePickupLocationCommand("Home", PickupLocationHandlerTestContext.Address(), false, context.Actor),
            CancellationToken.None);

        var crossSender = await context.UpdateHandler().HandleAsync(
            new UpdatePickupLocationCommand(
                created.Location!.Id,
                "Other",
                PickupLocationHandlerTestContext.Address(),
                context.Actor with { StakeholderId = Guid.CreateVersion7() }),
            CancellationToken.None);
        crossSender.Status.ShouldBe(UpdatePickupLocationStatus.NotFound);
    }

    [Fact]
    public async Task RejectUnauthenticatedCreate()
    {
        var context = new PickupLocationHandlerTestContext();

        var result = await context.CreateHandler().HandleAsync(
            new CreatePickupLocationCommand(
                "Home",
                PickupLocationHandlerTestContext.Address(),
                false,
                new ActorContext(null, null, "correlation", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(CreatePickupLocationStatus.NotAuthenticated);
        context.Repository.Locations.ShouldBeEmpty();
    }
}
