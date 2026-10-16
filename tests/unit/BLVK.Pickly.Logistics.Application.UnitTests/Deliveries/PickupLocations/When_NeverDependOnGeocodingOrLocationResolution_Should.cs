using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ReferencingSharedLocation_WithPickupLocationCommands_Should
{
[Theory]
    [InlineData(typeof(CreatePickupLocationHandler))]
    [InlineData(typeof(UpdatePickupLocationHandler))]
    public void NeverDependOnGeocodingOrLocationResolution(Type handlerType)
    {
        var dependencies = handlerType.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        dependencies.ShouldNotBeEmpty();
        dependencies.ShouldAllBe(dependency =>
            !ResolutionDependencies.Any(forbidden => forbidden.IsAssignableFrom(dependency)));
    }
}
