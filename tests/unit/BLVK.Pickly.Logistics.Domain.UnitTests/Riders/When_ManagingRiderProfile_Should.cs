using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Riders;

public sealed class When_ManagingRiderProfile_Should
{
    [Fact]
    public void RequireApprovalBeforeEnablingAvailability()
    {
        var profile = CreateProfile();

        Should.Throw<AggregateStateException>(() => profile.SetAvailability(true));

        profile.Approve();
        profile.SetAvailability(true);
        profile.AvailabilityStatus.ShouldBe(RiderAvailabilityStatus.Available);
    }

    [Fact]
    public void ForceOfflineWhenVehicleCategoryChanges()
    {
        var profile = CreateProfile();
        profile.Approve();
        profile.SetAvailability(true);

        var replacementCategoryId = Guid.CreateVersion7();
        profile.ChangeVehicleCategory(replacementCategoryId);

        profile.VehicleCategoryId.ShouldBe(replacementCategoryId);
        profile.AvailabilityStatus.ShouldBe(RiderAvailabilityStatus.Offline);
    }

    [Fact]
    public void ForceOfflineWhenSuspended()
    {
        var profile = CreateProfile();
        profile.Approve();
        profile.SetAvailability(true);

        profile.Suspend();

        profile.VerificationStatus.ShouldBe(RiderVerificationStatus.Suspended);
        profile.AvailabilityStatus.ShouldBe(RiderAvailabilityStatus.Offline);
    }

    private static RiderProfile CreateProfile() =>
        RiderProfile.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());
}
