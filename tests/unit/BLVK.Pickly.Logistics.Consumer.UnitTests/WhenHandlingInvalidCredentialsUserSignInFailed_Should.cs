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
using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

public sealed class WhenHandlingInvalidCredentialsUserSignInFailed_Should
{
    [Fact]
    public async Task IncrementFailedCountWithoutSendingAccountLockedNotification()
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
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var tenantId = Guid.CreateVersion7();
        var user = AppUser.Create(email, firstName, lastName);

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        identityService.FindByEmailAsync(email).Returns(user);
        identityService.AccessFailedAsync(user).Returns(IdentityResult.Success);
        identityService.IsLockedOutAsync(user).Returns(false);

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

        await identityService.Received(1).AccessFailedAsync(user);
        await commandSender.DidNotReceive().SendAsync(
            Arg.Any<SendNotificationCommand>(),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}





