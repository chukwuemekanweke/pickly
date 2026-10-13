using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Stakeholders.Profiles;

public sealed class When_GettingProfile_WithoutAuthenticatedStakeholder_Should
{
    [Fact]
    public async Task ReturnUnauthorized()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        currentActor.ActorId.Returns("anonymous");
        currentActor.TenantId.Returns(Guid.CreateVersion7());
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var logger = Substitute.For<ILogger<ProfilesController>>();
        var sut = CreateController(stakeholderReadModelRepository, currentActor, logger);

        var result = await sut.GetProfile(CancellationToken.None);

        result.Result.ShouldBeOfType<UnauthorizedResult>();
        var errorWasLogged = logger.ReceivedCalls().Any(call =>
        {
            var arguments = call.GetArguments();
            return call.GetMethodInfo().Name == nameof(ILogger.Log) &&
                Equals(arguments[0], LogLevel.Error) &&
                arguments[2]?.ToString()?.Contains("anonymous", StringComparison.Ordinal) == true;
        });
        errorWasLogged.ShouldBeTrue();
        await stakeholderReadModelRepository.DidNotReceiveWithAnyArgs()
            .GetByStakeholderIdAsync(default, default);
    }

    private static ProfilesController CreateController(
        IStakeholderReadModelRepository stakeholderReadModelRepository,
        ICurrentActor currentActor,
        ILogger<ProfilesController> logger)
    {
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        return ProfilesControllerTestFactory.Create(
            stakeholderReadModelRepository,
            stakeholderRepository,
            customTelemetryContext,
            unitOfWork,
            currentActor,
            logger: logger);
    }
}
