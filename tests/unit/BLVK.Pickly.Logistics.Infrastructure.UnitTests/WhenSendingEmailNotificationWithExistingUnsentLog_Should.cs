using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using BLVK.Pickly.Logistics.Domain.Notifications.Specifications;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Specifications;
using BLVK.Pickly.Logistics.Infrastructure.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class WhenSendingEmailNotificationWithExistingUnsentLog_Should
{
    [Fact]
    public async Task RetryDispatch()
    {
        var providerRepository = Substitute.For<IReadRepository<Provider>>();
        var templateRepository = Substitute.For<IReadRepository<EmailNotificationTemplate>>();
        var tenantRepository = Substitute.For<IReadRepository<Tenant>>();
        var logRepository = Substitute.For<IRepository<EmailNotificationLog>>();
        var transportProvider = Substitute.For<IEmailTransportProvider>();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var timeProvider = Substitute.For<TimeProvider>();
        var now = DateTimeOffset.UtcNow;
        var tenantId = Guid.CreateVersion7();
        var command = new SendNotificationCommand(
            tenantId,
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            NotificationMedium.Email,
            new EmailNotificationContent(
                InfrastructureTestData.Email(),
                new Dictionary<string, string>
                {
                    ["IpAddress"] = "127.0.0.1"
                }));
        var existingLog = EmailNotificationLog.Create(
            command.MessageId,
            command.TenantId,
            command.CountryId,
            command.NotificationType,
            [],
            ((EmailNotificationContent)command.NotificationContent).To,
            null,
            null,
            now);
        var templateRoot = Path.Combine(Path.GetTempPath(), $"email-templates-{Guid.CreateVersion7():N}");
        var tenantTemplateDirectory = Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "default", "en", "NotificationTypes");
        Directory.CreateDirectory(tenantTemplateDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "default", "en", "BaseTemplate.html"),
            "<html><body>{{:BodyHtml:}}</body></html>");
        await File.WriteAllTextAsync(
            Path.Combine(tenantTemplateDirectory, "SignInSuccessful.html"),
            "IP Address: {{:IpAddress:}}");

        logRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationLogByMessageIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(existingLog);
        providerRepository.FirstOrDefaultAsync(Arg.Any<ActiveProviderByTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(Provider.Create(ProviderType.Email, "Mailtrap", "mailtrap", true));
        templateRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationTemplateByNotificationTypeSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailNotificationTemplate.Create(NotificationType.SignInSuccessful, "Sign-in successful notification", "Subject {{:IpAddress:}}", "SignInSuccessful.html"));
        tenantRepository.FirstOrDefaultAsync(Arg.Any<TenantByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(Tenant.Create(tenantId, "Default", "default"));
        transportProvider.ProviderKey.Returns("mailtrap");
        transportProvider.SendAsync(Arg.Any<EmailDeliveryMessage>(), Arg.Any<CancellationToken>())
            .Returns(new EmailTransportSendResult("mailtrap-message-id"));
        hostEnvironment.ContentRootPath.Returns(templateRoot);
        timeProvider.GetUtcNow().Returns(now);

        var sut = new EmailNotificationDispatcher(
            providerRepository,
            templateRepository,
            tenantRepository,
            logRepository,
            [transportProvider],
            hostEnvironment,
            Options.Create(CreateOptions()),
            unitOfWork,
            timeProvider);

        await sut.SendAsync(command, CancellationToken.None);

        await logRepository.DidNotReceive().AddAsync(Arg.Any<EmailNotificationLog>(), Arg.Any<CancellationToken>());
        await transportProvider.Received(1).SendAsync(Arg.Any<EmailDeliveryMessage>(), Arg.Any<CancellationToken>());
        logRepository.Received(1).Update(Arg.Is<EmailNotificationLog>(log =>
            log.SentAtUtc == now &&
            log.ProviderMessageId == "mailtrap-message-id" &&
            log.FailureReason == null));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static EmailNotificationsOptions CreateOptions() =>
        new()
        {
            FromAddress = "no-reply@test.local",
            FromName = "Backend Project Template",
            TemplateSetsRootPath = "EmailTemplates/TemplateSets"
        };
}







