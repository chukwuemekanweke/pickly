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

public sealed class When_SendingEmailConfirmationOtp_WithNoActiveOtp_Should
{
    [Fact]
    public async Task GenerateAndQueueCode()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
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
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));
        var requestedAtUtc = clock.GetUtcNow().AddSeconds(-10);
        var expiresAtUtc = requestedAtUtc.Add(AuthenticationOtpDefaults.EmailConfirmationLifetime);
        twoFactorOtpService.GenerateOtpAsync(
                user.Id,
                OtpIntent.EmailConfirmation,
                Arg.Any<CancellationToken>(),
                6,
                false,
                expiresAtUtc)
            .Returns(new TwoFactorOtp("123456", expiresAtUtc));
        identityService.FindByIdAsync(user.Id).Returns(user);
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        stakeholderRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        var sender = new EmailConfirmationOtpSender(twoFactorOtpService, commandSender, clock);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        await new SendEmailConfirmationOtpHandler(
            Substitute.For<ICustomTelemetryContext>(),
            Substitute.For<ICurrentActorAccessor>(),
            messageContext,
            identityService,
            stakeholderRepository,
            sender,
            unitOfWork,
            clock,
            Substitute.For<ILogger<SendEmailConfirmationOtpHandler>>(),
            Substitute.For<IRepository<MessageInbox>>()).HandleAsync(
            new SendEmailConfirmationOtpCommand
            {
                StakeholderId = stakeholderId,
                TenantId = stakeholder.TenantId,
                RequestedAt = requestedAtUtc,
                ExpiresAtUtc = expiresAtUtc
            },
            CancellationToken.None);

        await twoFactorOtpService.Received(1).GenerateOtpAsync(
            user.Id,
            OtpIntent.EmailConfirmation,
            Arg.Any<CancellationToken>(),
            6,
            false,
            expiresAtUtc);
        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command => command.StakeholderId == stakeholderId),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
