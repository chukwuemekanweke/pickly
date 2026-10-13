using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using BLVK.Pickly.Logistics.Domain.Notifications.Specifications;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class WhenSendingEmailNotificationFails_Should
{
    [Fact]
    public async Task PersistFailureReasonInLog()
    {
        var providerRepository = Substitute.For<IReadRepository<Provider>>();
        var templateRepository = Substitute.For<IReadRepository<EmailNotificationTemplate>>();
        var tenantRepository = Substitute.For<IReadRepository<Tenant>>();
        var logRepository = Substitute.For<IRepository<EmailNotificationLog>>();
        var transportProvider = Substitute.For<IEmailTransportProvider>();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var now = DateTimeOffset.UtcNow;
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(now);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var options = Options.Create(new EmailNotificationsOptions
        {
            FromAddress = "no-reply@test.local",
            FromName = "Backend Project Template",
            TemplateSetsRootPath = "EmailTemplates/TemplateSets"
        });

        var command = new SendNotificationCommand(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.AccountLocked,
            NotificationMedium.Email,
            new EmailNotificationContent(
                InfrastructureTestData.Email(),
                new Dictionary<string, string>
                {
                    ["LockedUntilUtc"] = "now"
                }));

        providerRepository.FirstOrDefaultAsync(Arg.Any<ActiveProviderByTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(Provider.Create(ProviderType.Email, "Mailtrap", "mailtrap", true));
        templateRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationTemplateByNotificationTypeSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns((EmailNotificationTemplate?)null);
        transportProvider.ProviderKey.Returns("mailtrap");

        var sut = new EmailNotificationDispatcher(
            providerRepository,
            templateRepository,
            tenantRepository,
            logRepository,
            [transportProvider],
            hostEnvironment,
            options,
            unitOfWork,
            timeProvider);

        var exception = await Should.ThrowAsync<NotificationConfigurationException>(() =>
            sut.SendAsync(command, CancellationToken.None));

        exception.Message.ShouldBe("No 'en' email template is configured for notification type 'AccountLocked'.");
        logRepository.Received(1).Update(Arg.Is<EmailNotificationLog>(log =>
            log.SentAtUtc == null &&
            log.FailureReason == "No 'en' email template is configured for notification type 'AccountLocked'."));
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}




