using BLVK.Pickly.Logistics.Consumer.Authentication;
using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Formatting;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

public sealed class WhenHandlingUserCreated_Should
{
    [Fact]
    public async Task GenerateSignUpOtpAndQueueNotificationCommand()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var twoFactorOtpService = Substitute.For<ITwoFactorOtpService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var messageInboxRepository = Substitute.For<IRepository<MessageInbox>>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var logger = Substitute.For<ILogger<UserCreatedHandler>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 23, 10, 0, 0, TimeSpan.Zero));
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var otpCode = ConsumerTestData.Otp();
        var requestedAtUtc = timeProvider.GetUtcNow().AddSeconds(-10);
        var expiresAtUtc = requestedAtUtc.Add(AuthenticationOtpDefaults.EmailConfirmationLifetime);
        var user = AppUser.Create(email, firstName, lastName);

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, user.Id, email, tenantId, countryId, Guid.CreateVersion7(), firstName, lastName, null, false));
        identityService.FindByIdAsync(user.Id).Returns(user);
        var otp = new TwoFactorOtp(
            otpCode,
            expiresAtUtc);
        twoFactorOtpService.OtpExistsAsync(user.Id, OtpIntent.EmailConfirmation, Arg.Any<CancellationToken>())
            .Returns(false);
        twoFactorOtpService.GenerateOtpAsync(
                user.Id,
                OtpIntent.EmailConfirmation,
                Arg.Any<CancellationToken>(),
                6,
                false,
                expiresAtUtc)
            .Returns(otp);
        var otpSender = new EmailConfirmationOtpSender(
            twoFactorOtpService,
            commandSender,
            ConsumerTestData.RegistrationPolicyProvider(),
            timeProvider);

        await new UserCreatedHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            identityService,
            stakeholderReadModelRepository,
            otpSender,
            unitOfWork,
            timeProvider,
            logger,
            messageInboxRepository).HandleAsync(
            new UserCreated
            {
                StakeholderId = stakeholderId,
                TenantId = tenantId,
                RequestedAtUtc = requestedAtUtc,
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
        await identityService.Received(1).FindByIdAsync(user.Id);
        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command => HasExpectedNotificationCommand(
                command,
                tenantId,
                countryId,
                stakeholderId,
                email,
                firstName,
                lastName,
                otpCode,
                DateTimeFormatter.FormatHumanReadableUtc(otp.ExpiresAtUtc, timeProvider.GetUtcNow()))),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static bool HasExpectedNotificationCommand(
        SendNotificationCommand command,
        Guid tenantId,
        Guid countryId,
        Guid stakeholderId,
        string email,
        string firstName,
        string lastName,
        string otpCode,
        string otpExpiresAtUtc)
    {
        if (command.NotificationContent is not EmailNotificationContent content)
        {
            return false;
        }

        return command.TenantId == tenantId &&
            command.CountryId == countryId &&
            command.NotificationType == NotificationType.EmailConfirmationOtp &&
            command.NotificationMedium == NotificationMedium.Email &&
            command.StakeholderId == stakeholderId &&
            content.To == email &&
            content.Content["FirstName"] == firstName &&
            content.Content["LastName"] == lastName &&
            content.Content["OtpCode"] == otpCode &&
            content.Content["OtpExpiresAtUtc"] == otpExpiresAtUtc &&
            content.Content["Product"] == "BLVK.Pickly.Logistics";
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}





