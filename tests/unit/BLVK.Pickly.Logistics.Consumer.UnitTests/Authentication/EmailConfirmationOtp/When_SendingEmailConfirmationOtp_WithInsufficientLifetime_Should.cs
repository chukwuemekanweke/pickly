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

public sealed class When_SendingEmailConfirmationOtp_WithInsufficientLifetime_Should
{
    [Fact]
    public async Task SkipGeneratingAndCaptureQueueDelay()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var user = AppUser.Create(ConsumerTestData.Email());
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
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 8, 22, 12, 4, 30, TimeSpan.Zero));
        var requestedAtUtc = clock.GetUtcNow().AddMinutes(-4).AddSeconds(-30);
        var expiresAtUtc = requestedAtUtc.Add(AuthenticationOtpDefaults.EmailConfirmationLifetime);
        identityService.FindByIdAsync(user.Id).Returns(user);
        stakeholderRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));

        await new SendEmailConfirmationOtpHandler(
            customTelemetryContext,
            Substitute.For<ICurrentActorAccessor>(),
            messageContext,
            identityService,
            stakeholderRepository,
            new EmailConfirmationOtpSender(twoFactorOtpService, commandSender, clock),
            Substitute.For<IUnitOfWork>(),
            clock,
            Substitute.For<ILogger<SendEmailConfirmationOtpHandler>>(),
            Substitute.For<IRepository<MessageInbox>>()).HandleAsync(
            new SendEmailConfirmationOtpCommand
            {
                StakeholderId = stakeholderId,
                TenantId = tenantId,
                RequestedAt = requestedAtUtc,
                ExpiresAtUtc = expiresAtUtc
            },
            CancellationToken.None);

        await twoFactorOtpService.Received(1).OtpExistsAsync(
            user.Id,
            OtpIntent.EmailConfirmation,
            Arg.Any<CancellationToken>());
        await twoFactorOtpService.DidNotReceiveWithAnyArgs().GenerateOtpAsync(
            default,
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
                properties[Observability.PropertyNames.Common.FailureReason] == ObservabilityFailureReasons.InsufficientOtpLifetime &&
                properties[Observability.PropertyNames.Common.QueueDelayMilliseconds] == "270000" &&
                properties[Observability.PropertyNames.Common.RemainingLifetimeMilliseconds] == "30000"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
