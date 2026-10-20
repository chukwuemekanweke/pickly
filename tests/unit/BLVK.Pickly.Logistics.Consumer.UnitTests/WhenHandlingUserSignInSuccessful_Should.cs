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

public sealed class WhenHandlingUserSignInSuccessful_Should
{
    [Fact]
    public async Task ResetFailedCountAndSendNotification()
    {
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var commandSender = Substitute.For<ICommandSender>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var loginActivityIpAddressResolver = Substitute.For<ILoginActivityIpAddressResolver>();
        var loginActivityRepository = Substitute.For<IRepository<LoginActivity>>();
        var userAgentParserService = Substitute.For<IUserAgentParserService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var messageInboxRepository = Substitute.For<IRepository<MessageInbox>>();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var email = ConsumerTestData.Email();
        var ipAddress = ConsumerTestData.IpAddress();
        var userAgent = ConsumerTestData.UserAgent();
        var firstName = ConsumerTestData.FirstName();
        var lastName = ConsumerTestData.LastName();
        var user = AppUser.Create(email, firstName, lastName);
        var appUserId = Guid.CreateVersion7();

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        identityService.FindByIdAsync(appUserId).Returns(user);
        identityService.ResetAccessFailedCountAsync(user).Returns(IdentityResult.Success);
        stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, appUserId, email, tenantId, countryId, Guid.CreateVersion7(), "Ada", "Lovelace", null, false));
        userAgentParserService.Parse(userAgent).Returns(new UserAgentInfo("Desktop", "Windows", "Chrome"));
        loginActivityIpAddressResolver.ResolveAsync(ipAddress, Arg.Any<CancellationToken>())
            .Returns(new LoginActivityIpAddressResolution(Guid.CreateVersion7(), null));

        await new UserSignInSuccessfulHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            identityService,
            stakeholderReadModelRepository,
            commandSender,
            loginActivityIpAddressResolver,
            loginActivityRepository,
            unitOfWork,
            userAgentParserService,
            TimeProvider.System,
            messageInboxRepository,
            Substitute.For<ILogger<UserSignInSuccessfulHandler>>()).HandleAsync(
            new UserSignInSuccessful(ipAddress, userAgent)
            {
                StakeholderId = stakeholderId,
                TenantId = tenantId
            },
            CancellationToken.None);

        await identityService.Received(1).ResetAccessFailedCountAsync(user);
        await commandSender.Received(1).SendAsync(
            Arg.Is<SendNotificationCommand>(command =>
                command.TenantId == tenantId &&
                command.CountryId == countryId &&
                command.NotificationType == NotificationType.SignInSuccessful &&
                command.NotificationMedium == NotificationMedium.Email &&
                command.StakeholderId == stakeholderId &&
                command.NotificationContent is EmailNotificationContent &&
                ((EmailNotificationContent)command.NotificationContent).To == email &&
                ((EmailNotificationContent)command.NotificationContent).Content["IllustrationUrl"] == NotificationIllustrationUrls.SignInSuccessful &&
                ((EmailNotificationContent)command.NotificationContent).Content["IpAddress"] == ipAddress &&
                ((EmailNotificationContent)command.NotificationContent).Content["UserAgent"] == userAgent),
            Arg.Any<CancellationToken>());
        await loginActivityIpAddressResolver.Received(1).ResolveAsync(ipAddress, Arg.Any<CancellationToken>());
        await loginActivityRepository.Received(1).AddAsync(Arg.Any<LoginActivity>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}





