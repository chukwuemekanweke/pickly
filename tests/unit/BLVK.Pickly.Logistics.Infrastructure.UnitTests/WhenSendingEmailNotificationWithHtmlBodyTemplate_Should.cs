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

public sealed class WhenSendingEmailNotificationWithHtmlBodyTemplate_Should
{
    [Fact]
    public async Task RenderHtmlWithoutEscaping()
    {
        var tenantId = Guid.CreateVersion7();
        var templateRoot = Path.Combine(Path.GetTempPath(), $"email-templates-{Guid.CreateVersion7():N}");
        var tenantTemplateDirectory = Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "moveaex", "en", "NotificationTypes");
        Directory.CreateDirectory(tenantTemplateDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "moveaex", "en", "BaseTemplate.html"),
            "<html><body>{{:BodyHtml:}}</body></html>");
        await File.WriteAllTextAsync(
            Path.Combine(tenantTemplateDirectory, "AccountCreated.html"),
            "<div>Hello {{:FirstName:}}</div><div>Line 2</div>");

        var providerRepository = Substitute.For<IReadRepository<Provider>>();
        var templateRepository = Substitute.For<IReadRepository<EmailNotificationTemplate>>();
        var tenantRepository = Substitute.For<IReadRepository<Tenant>>();
        var emailNotificationLogRepository = Substitute.For<IRepository<EmailNotificationLog>>();
        var transportProvider = Substitute.For<IEmailTransportProvider>();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var options = Options.Create(new EmailNotificationsOptions
        {
            FromAddress = "no-reply@test.local",
            FromName = "Backend Project Template",
            TemplateSetsRootPath = "EmailTemplates/TemplateSets"
        });
        var now = DateTimeOffset.UtcNow;
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(now);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var command = new SendNotificationCommand(
            tenantId,
            Guid.CreateVersion7(),
            NotificationType.AccountCreated,
            NotificationMedium.Email,
            new EmailNotificationContent(
                InfrastructureTestData.Email(),
                new Dictionary<string, string>
                {
                    ["FirstName"] = "Ada"
                }));

        providerRepository.FirstOrDefaultAsync(Arg.Any<ActiveProviderByTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(Provider.Create(ProviderType.Email, "Mailtrap", "mailtrap", true));
        templateRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationTemplateByNotificationTypeSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailNotificationTemplate.Create(NotificationType.AccountCreated, "Account created notification", "Welcome", "AccountCreated.html"));
        tenantRepository.FirstOrDefaultAsync(
                Arg.Any<TenantByIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(Tenant.Create(tenantId, "Moveaex", "moveaex"));
        transportProvider.ProviderKey.Returns("mailtrap");
        transportProvider.SendAsync(Arg.Any<EmailDeliveryMessage>(), Arg.Any<CancellationToken>())
            .Returns(new EmailTransportSendResult("mailtrap-message-id"));
        hostEnvironment.ContentRootPath.Returns(templateRoot);

        var sut = new EmailNotificationDispatcher(
            providerRepository,
            templateRepository,
            tenantRepository,
            emailNotificationLogRepository,
            [transportProvider],
            hostEnvironment,
            options,
            unitOfWork,
            timeProvider);

        await sut.SendAsync(command, CancellationToken.None);

        await transportProvider.Received(1).SendAsync(
            Arg.Is<EmailDeliveryMessage>(message =>
                message.Subject == "Welcome" &&
                message.HtmlBody == "<html><body><div>Hello Ada</div><div>Line 2</div></body></html>"),
            Arg.Any<CancellationToken>());
    }
}









