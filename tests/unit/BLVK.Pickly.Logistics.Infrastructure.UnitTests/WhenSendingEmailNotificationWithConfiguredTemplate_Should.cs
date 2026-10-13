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

public sealed class WhenSendingEmailNotificationWithConfiguredTemplate_Should
{
    [Fact]
    public async Task RenderAndDispatchEmail()
    {
        var tenantId = Guid.CreateVersion7();
        var templateRoot = Path.Combine(Path.GetTempPath(), $"email-templates-{Guid.CreateVersion7():N}");
        const string language = "fr";
        var tenantTemplateDirectory = Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "moveaex", language, "NotificationTypes");
        Directory.CreateDirectory(tenantTemplateDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "moveaex", language, "BaseTemplate.html"),
            "<html><body><header>{{:Subject:}}</header>{{:BodyHtml:}}<footer>Tenant Footer</footer></body></html>");
        await File.WriteAllTextAsync(
            Path.Combine(tenantTemplateDirectory, "SignInSuccessful.html"),
            "Une connexion à votre compte a réussi.\nAdresse IP : {{:IpAddress:}}\nAgent utilisateur : {{:UserAgent:}}");

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
            NotificationType.SignInSuccessful,
            NotificationMedium.Email,
            new EmailNotificationContent(
                InfrastructureTestData.Email(),
                new Dictionary<string, string>
                {
                    ["IpAddress"] = "127.0.0.1",
                    ["UserAgent"] = "Test Agent"
                }))
        {
            Language = language
        };

        providerRepository.FirstOrDefaultAsync(Arg.Any<ActiveProviderByTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(Provider.Create(ProviderType.Email, "Mailtrap", "mailtrap", true));
        templateRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationTemplateByNotificationTypeSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailNotificationTemplate.Create(NotificationType.SignInSuccessful, language, "Notification de connexion réussie", "Connexion réussie depuis {{:IpAddress:}}", "SignInSuccessful.html"));
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
                message.Subject == "Connexion réussie depuis 127.0.0.1" &&
                message.To == ((EmailNotificationContent)command.NotificationContent).To &&
                message.HtmlBody == "<html><body><header>Connexion r&#233;ussie depuis 127.0.0.1</header><p>Une connexion &#224; votre compte a r&#233;ussi.</p><p>Adresse IP : 127.0.0.1</p><p>Agent utilisateur : Test Agent</p><footer>Tenant Footer</footer></body></html>"),
            Arg.Any<CancellationToken>());

        await emailNotificationLogRepository.Received(1).AddAsync(
            Arg.Is<EmailNotificationLog>(log => log.Language == language),
            Arg.Any<CancellationToken>());
    }
}









