using BLVK.Pickly.Logistics.Application.Stakeholders.Features.UpdateProfile;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Stakeholders.UpdateProfile;

public sealed class When_UpdatingProfile_WithAnonymousActor_Should
{
    [Fact]
    public async Task ReturnNotAuthenticated()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        currentActor.ActorId.Returns(string.Empty);

        var sut = new UpdateProfileHandler(
            Substitute.For<IRepository<Stakeholder>>(),
            customTelemetryContext,
            Substitute.For<IUnitOfWork>());

        var result = await sut.HandleAsync(
            new UpdateProfileCommand("Jane", "Doe", new ActorContext(null, Guid.CreateVersion7(), Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(UpdateProfileStatus.NotAuthenticated);
        customTelemetryContext.Received().AddCustomEvent(
            Observability.EventNames.Authentication.ProfileUpdateFailed,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Common.FailureReason] == ObservabilityFailureReasons.NotAuthenticated));
    }
}
