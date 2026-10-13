using BLVK.Pickly.Logistics.Consumer.Authentication;
using BLVK.Pickly.Logistics.Contracts.Commands.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

public sealed class WhenHandlingResetPasswordWithActiveOtp_Should
{
    [Fact]
    public async Task NotSendNotification()
    {
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var messageInboxRepository = Substitute.For<IRepository<MessageInbox>>();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var appUserId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        twoFactorOtpService.OtpExistsAsync(appUserId, OtpIntent.PasswordReset, Arg.Any<CancellationToken>())
            .Returns(true);
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, appUserId, email, tenantId, countryId, Guid.CreateVersion7(), ConsumerTestData.FirstName(), ConsumerTestData.LastName(), null, false));

        await new ResetPasswordHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            twoFactorOtpService,
            stakeholderReadModelRepository,
            commandSender,
            unitOfWork,
            TimeProvider.System,
            messageInboxRepository,
            Substitute.For<ILogger<ResetPasswordHandler>>()).HandleAsync(
            new ResetPasswordCommand
            {
                StakeholderId = stakeholderId,
                TenantId = tenantId
            },
            CancellationToken.None);

        await twoFactorOtpService.DidNotReceive().GenerateOtpAsync(
            Arg.Any<Guid>(),
            Arg.Any<OtpIntent>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<int>(),
            Arg.Any<bool>());
        await commandSender.DidNotReceive().SendAsync(Arg.Any<Contracts.Commands.Notifications.SendNotificationCommand>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

