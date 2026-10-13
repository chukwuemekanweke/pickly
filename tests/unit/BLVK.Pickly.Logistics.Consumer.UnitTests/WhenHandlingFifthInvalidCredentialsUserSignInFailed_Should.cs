using BLVK.Pickly.Logistics.Consumer.Authentication;
using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Formatting;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

public sealed class WhenHandlingFifthInvalidCredentialsUserSignInFailed_Should
{
    [Fact]
    public async Task SendAccountLockedNotification()
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
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var ipAddress = ConsumerTestData.IpAddress();
        var userAgent = ConsumerTestData.UserAgent();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 7, 10, 0, 0, TimeSpan.Zero));
        var lockedUntilUtc = new DateTimeOffset(2026, 4, 7, 12, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(email, firstName, lastName);

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        identityService.FindByEmailAsync(email).Returns(user);
        identityService.AccessFailedAsync(user).Returns(IdentityResult.Success);
        identityService.IsLockedOutAsync(user).Returns(true);
        identityService.GetLockoutEndUtcAsync(user).Returns(lockedUntilUtc);
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, Guid.CreateVersion7(), email, tenantId, countryId, Guid.CreateVersion7(), "Ada", "Lovelace", null, false));

        var sessionService = Substitute.For<IAuthenticationSessionService>();
        await new UserSignInFailedHandler(customTelemetryContext, currentActorAccessor, messageContext, identityService, sessionService, stakeholderReadModelRepository, commandSender, unitOfWork, timeProvider, logger, messageInboxRepository).HandleAsync(
            new UserSignInFailed(
                email,
                ipAddress,
                userAgent,
                UserSignInFailureReasons.InvalidCredentials)
            {
                StakeholderId = stakeholderId,
                TenantId = tenantId
            },
            CancellationToken.None);

        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command =>
                command.TenantId == tenantId &&
                command.CountryId == countryId &&
                command.NotificationType == NotificationType.AccountLocked &&
                command.NotificationMedium == NotificationMedium.Email &&
                command.StakeholderId == stakeholderId &&
                command.NotificationContent is EmailNotificationContent &&
                ((EmailNotificationContent)command.NotificationContent).To == email &&
                ((EmailNotificationContent)command.NotificationContent).Content["LockedUntilUtc"] == DateTimeFormatter.FormatHumanReadableUtc(lockedUntilUtc, timeProvider.GetUtcNow())),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await sessionService.Received(1).RevokeAllAsync(stakeholderId, Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}





