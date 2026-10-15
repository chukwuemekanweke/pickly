using BLVK.Pickly.Logistics.Application.Deliveries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.PickupLocations;

public sealed class When_HandlingPickupLocationRequests_Should
{
    [Fact]
    public async Task ReturnCreatedLocationForValidPayload()
    {
        var context = new PickupLocationsControllerTestContext();

        var result = await context.CreateController().Create(
            PickupLocationsControllerTestContext.CreateRequest(),
            CancellationToken.None);

        var created = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        created.Value.ShouldBeOfType<PickupLocationResponse>().IsDefault.ShouldBeTrue();
        await context.Repository.Received(1).AddAsync(
            Arg.Any<SavedPickupLocation>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnBadRequestForInvalidPayload()
    {
        var context = new PickupLocationsControllerTestContext();
        var request = PickupLocationsControllerTestContext.CreateRequest("");

        var result = await context.CreateController().Create(request, CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
        await context.Repository.DidNotReceive().AddAsync(
            Arg.Any<SavedPickupLocation>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnCursorPage()
    {
        var context = new PickupLocationsControllerTestContext();
        var model = ReadModel();
        context.ReadRepository.ListAsync(
                Arg.Any<SavedPickupLocationsCursorRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new SavedPickupLocationsCursorPage([model], false));

        var result = await context.CreateController().List(
            new ListPickupLocationsRequest(),
            CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<ListPickupLocationsResponse>().Items.Single().Id.ShouldBe(model.Id);
    }

    [Fact]
    public async Task ReturnNoContentWhenDefaultDoesNotExist()
    {
        var context = new PickupLocationsControllerTestContext();

        var result = await context.CreateController().GetDefault(CancellationToken.None);

        result.Result.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ReturnNotFoundForLocationOutsideScope()
    {
        var context = new PickupLocationsControllerTestContext();

        var result = await context.CreateController().GetById(Guid.CreateVersion7(), CancellationToken.None);

        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateOwnedLocation()
    {
        var context = new PickupLocationsControllerTestContext();
        var location = context.Location(isDefault: true);
        context.Repository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<SavedPickupLocation>>(),
                Arg.Any<CancellationToken>())
            .Returns(location);

        var result = await context.CreateController().Update(
            location.Id,
            new UpdatePickupLocationRequest("Office", PickupLocationsControllerTestContext.Address("Suite C9")),
            CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
        location.Label.ShouldBe("Office");
        location.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task SetOwnedLocationAsDefault()
    {
        var context = new PickupLocationsControllerTestContext();
        var target = context.Location("Office");
        var current = context.Location("Home", true);
        context.Repository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<SavedPickupLocation>>(),
                Arg.Any<CancellationToken>())
            .Returns(target, current);

        var result = await context.CreateController().SetDefault(target.Id, CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
        target.IsDefault.ShouldBeTrue();
        current.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteOwnedLocationAndSelectReplacement()
    {
        var context = new PickupLocationsControllerTestContext();
        var target = context.Location("Home", true);
        var replacement = context.Location("Office");
        context.Repository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<SavedPickupLocation>>(),
                Arg.Any<CancellationToken>())
            .Returns(target, replacement);

        var result = await context.CreateController().Delete(target.Id, CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
        target.IsDeleted.ShouldBeTrue();
        replacement.IsDefault.ShouldBeTrue();
    }

    private static SavedPickupLocationReadModel ReadModel() =>
        new(
            Guid.CreateVersion7(),
            "Home",
            "36 Garden Avenue",
            null,
            "Enugu",
            "Enugu",
            "NG",
            6.4474m,
            7.5139m,
            true,
            DateTimeOffset.UtcNow);
}
