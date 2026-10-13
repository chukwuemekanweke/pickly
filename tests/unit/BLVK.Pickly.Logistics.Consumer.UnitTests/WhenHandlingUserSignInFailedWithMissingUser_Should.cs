using BLVK.Pickly.Logistics.Consumer.Authentication;
using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

public sealed class WhenHandlingUserSignInFailedWithMissingUser_Should
{
    [Fact]
    public async Task ThrowNonTransientException()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var messageInboxRepository = Substitute.For<IRepository<MessageInbox>>();
        var logger = Substitute.For<ILogger<UserSignInFailedHandler>>();
        var email = ConsumerTestData.Email();
        var ipAddress = ConsumerTestData.IpAddress();
        var userAgent = ConsumerTestData.UserAgent();
        var tenantId = Guid.CreateVersion7();

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        identityService.FindByEmailAsync(email).Returns((AppUser?)null);

        await new UserSignInFailedHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            identityService,
            Substitute.For<IAuthenticationSessionService>(),
            stakeholderReadModelRepository,
            commandSender,
            unitOfWork,
            TimeProvider.System,
            logger,
            messageInboxRepository).HandleAsync(
                new UserSignInFailed(
                    email,
                    ipAddress,
                    userAgent,
                    UserSignInFailureReasons.InvalidCredentials)
                {
                    TenantId = tenantId
                },
                CancellationToken.None);

        await commandSender.DidNotReceive().SendAsync(Arg.Any<SendNotificationCommand>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

