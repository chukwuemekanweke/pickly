using BLVK.Pickly.Logistics.Consumer.Authentication;
using BLVK.Pickly.Logistics.Contracts.Commands.Authentication;
using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Formatting;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

public sealed class WhenHandlingResetPassword_Should
{
    [Fact]
    public async Task GenerateOtpAndSendNotification()
    {
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var messageInboxRepository = Substitute.For<IRepository<MessageInbox>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 23, 10, 0, 0, TimeSpan.Zero));
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var appUserId = Guid.CreateVersion7();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var email = ConsumerTestData.Email();
        var otp = new TwoFactorOtp(ConsumerTestData.Otp(), timeProvider.GetUtcNow().AddMinutes(2));

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        twoFactorOtpService.OtpExistsAsync(appUserId, OtpIntent.PasswordReset, Arg.Any<CancellationToken>())
            .Returns(false);
        twoFactorOtpService.GenerateOtpAsync(
                appUserId,
                OtpIntent.PasswordReset,
                Arg.Any<CancellationToken>(),
                6,
                false)
            .Returns(otp);
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, appUserId, email, tenantId, countryId, Guid.CreateVersion7(), firstName, lastName, null, false));

        await new ResetPasswordHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            twoFactorOtpService,
            stakeholderReadModelRepository,
            commandSender,
            unitOfWork,
            timeProvider,
            messageInboxRepository,
            Substitute.For<ILogger<ResetPasswordHandler>>()).HandleAsync(
            new ResetPasswordCommand
            {
                StakeholderId = stakeholderId,
                TenantId = tenantId
            },
            CancellationToken.None);

        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command =>
                command.TenantId == tenantId &&
                command.CountryId == countryId &&
                command.NotificationType == NotificationType.ResetPasswordOtp &&
                command.NotificationMedium == NotificationMedium.Email &&
                command.StakeholderId == stakeholderId &&
                command.NotificationContent is EmailNotificationContent &&
                ((EmailNotificationContent)command.NotificationContent).To == email &&
                ((EmailNotificationContent)command.NotificationContent).Content["FirstName"] == firstName &&
                ((EmailNotificationContent)command.NotificationContent).Content["LastName"] == lastName &&
                ((EmailNotificationContent)command.NotificationContent).Content["OtpCode"] == otp.Code &&
                ((EmailNotificationContent)command.NotificationContent).Content["OtpExpiresAtUtc"] == DateTimeFormatter.FormatHumanReadableUtc(otp.ExpiresAtUtc, timeProvider.GetUtcNow())),
            Arg.Any<CancellationToken>());
        await twoFactorOtpService.Received(1).GenerateOtpAsync(
            appUserId,
            OtpIntent.PasswordReset,
            Arg.Any<CancellationToken>(),
            6,
            false);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

