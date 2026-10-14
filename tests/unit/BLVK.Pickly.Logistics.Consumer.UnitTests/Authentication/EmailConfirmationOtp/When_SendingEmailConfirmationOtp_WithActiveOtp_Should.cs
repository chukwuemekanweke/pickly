using BLVK.Pickly.Logistics.Consumer.Authentication;
using BLVK.Pickly.Logistics.Contracts.Commands.Authentication;
using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Authentication.EmailConfirmationOtp;

public sealed class When_SendingEmailConfirmationOtp_WithActiveOtp_Should
{
    [Fact]
    public async Task SkipGeneratingAndQueuingCode()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderId = Guid.CreateVersion7();
        var user = AppUser.Create(ConsumerTestData.Email());
        var stakeholder = new StakeholderReadModel(
            stakeholderId,
            user.Id,
            user.Email!,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            ConsumerTestData.FirstName(),
            ConsumerTestData.LastName(),
            null,
            false);
        identityService.FindByIdAsync(user.Id).Returns(user);
        stakeholderRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        twoFactorOtpService.OtpExistsAsync(
                user.Id,
                OtpIntent.EmailConfirmation,
                Arg.Any<CancellationToken>())
            .Returns(true);
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        var clock = TimeProvider.System;
        var sender = new EmailConfirmationOtpSender(
            twoFactorOtpService,
            commandSender,
            ConsumerTestData.RegistrationPolicyProvider(),
            clock);

        await new SendEmailConfirmationOtpHandler(
            customTelemetryContext,
            Substitute.For<ICurrentActorAccessor>(),
            messageContext,
            identityService,
            stakeholderRepository,
            sender,
            Substitute.For<IUnitOfWork>(),
            clock,
            Substitute.For<ILogger<SendEmailConfirmationOtpHandler>>(),
            Substitute.For<IRepository<MessageInbox>>()).HandleAsync(
            new SendEmailConfirmationOtpCommand
            {
                StakeholderId = stakeholderId,
                TenantId = stakeholder.TenantId,
                RequestedAt = clock.GetUtcNow(),
                ExpiresAtUtc = clock.GetUtcNow().Add(AuthenticationOtpDefaults.EmailConfirmationLifetime)
            },
            CancellationToken.None);

        await twoFactorOtpService.DidNotReceiveWithAnyArgs().GenerateOtpAsync(
            default,
            default,
            default,
            default,
            default);
        await commandSender.DidNotReceiveWithAnyArgs().SendAsync(
            default(SendNotificationCommand)!,
            default);
        customTelemetryContext.Received(1).AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationOtpSendSkipped,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Common.StakeholderId] == stakeholderId.ToString() &&
                properties[Observability.PropertyNames.Common.FailureReason] == ObservabilityFailureReasons.ActiveOtpExists));
    }
}
