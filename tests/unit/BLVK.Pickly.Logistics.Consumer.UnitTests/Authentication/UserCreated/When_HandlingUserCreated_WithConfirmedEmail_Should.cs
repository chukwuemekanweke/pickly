using BLVK.Pickly.Logistics.Consumer.Authentication;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using UserCreatedEvent = BLVK.Pickly.Logistics.Contracts.Events.UserCreated;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Authentication.UserCreated;

public sealed class When_HandlingUserCreated_WithConfirmedEmail_Should
{
    [Fact]
    public async Task CaptureSkippedEventWithReason()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var user = AppUser.Create(ConsumerTestData.Email());
        user.MarkEmailVerified();
        var stakeholder = new StakeholderReadModel(
            stakeholderId,
            user.Id,
            user.Email!,
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            ConsumerTestData.FirstName(),
            ConsumerTestData.LastName(),
            null,
            false);
        identityService.FindByIdAsync(user.Id).Returns(user);
        stakeholderRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        var clock = TimeProvider.System;

        await new UserCreatedHandler(
            customTelemetryContext,
            Substitute.For<ICurrentActorAccessor>(),
            messageContext,
            identityService,
            stakeholderRepository,
            new EmailConfirmationOtpSender(twoFactorOtpService, Substitute.For<ICommandSender>(), clock),
            Substitute.For<IUnitOfWork>(),
            clock,
            Substitute.For<ILogger<UserCreatedHandler>>(),
            Substitute.For<IRepository<MessageInbox>>()).HandleAsync(
            new UserCreatedEvent
            {
                StakeholderId = stakeholderId,
                TenantId = tenantId,
                RequestedAtUtc = clock.GetUtcNow(),
                ExpiresAtUtc = clock.GetUtcNow().Add(AuthenticationOtpDefaults.EmailConfirmationLifetime)
            },
            CancellationToken.None);

        await twoFactorOtpService.DidNotReceiveWithAnyArgs().OtpExistsAsync(default, default, default);
        customTelemetryContext.Received(1).AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationOtpSendSkipped,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Common.StakeholderId] == stakeholderId.ToString() &&
                properties[Observability.PropertyNames.Common.FailureReason] == ObservabilityFailureReasons.AlreadyConfirmed));
    }
}
