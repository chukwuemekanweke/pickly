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
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class WhenSendingEmailNotification_Should
{
    [Fact]
    public async Task PersistPendingLogBeforeDispatch()
    {
        var tenantId = Guid.CreateVersion7();
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
        Guid loggedMessageId = Guid.Empty;
        string loggedTo = string.Empty;
        string? loggedCc = null;
        string? loggedBcc = null;
        DateTimeOffset loggedEnqueuedAtUtc = DateTimeOffset.MinValue;
        DateTimeOffset? loggedSentAtUtc = DateTimeOffset.MaxValue;
        string? loggedProviderMessageId = "placeholder";
        string? loggedFailureReason = "placeholder";
        Guid loggedTenantId = Guid.Empty;
        Guid loggedCountryId = Guid.Empty;
        NotificationType loggedNotificationType = default;
        Dictionary<string, string> loggedNotificationContent = [];
        logRepository
            .When(repository => repository.AddAsync(Arg.Any<EmailNotificationLog>(), Arg.Any<CancellationToken>()))
            .Do(callInfo =>
            {
                var log = callInfo.Arg<EmailNotificationLog>();
                loggedMessageId = log.MessageId;
                loggedTenantId = log.TenantId;
                loggedCountryId = log.CountryId;
                loggedNotificationType = log.NotificationType;
                loggedNotificationContent = new Dictionary<string, string>(log.NotificationContent);
                loggedTo = log.To;
                loggedCc = log.Cc;
                loggedBcc = log.Bcc;
                loggedEnqueuedAtUtc = log.EnqueuedAtUtc;
                loggedSentAtUtc = log.SentAtUtc;
                loggedProviderMessageId = log.ProviderMessageId;
                loggedFailureReason = log.FailureReason;
            });

        var templateRoot = Path.Combine(Path.GetTempPath(), $"email-templates-{Guid.CreateVersion7():N}");
        var tenantTemplateDirectory = Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "moveaex", "en", "NotificationTypes");
        Directory.CreateDirectory(tenantTemplateDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(templateRoot, "EmailTemplates", "TemplateSets", "moveaex", "en", "BaseTemplate.html"),
            "<html><body>{{:BodyHtml:}}</body></html>");
        await File.WriteAllTextAsync(
            Path.Combine(tenantTemplateDirectory, "SignInSuccessful.html"),
            "IP Address: {{:IpAddress:}}");

        var options = Options.Create(new EmailNotificationsOptions
        {
            FromAddress = "no-reply@test.local",
            FromName = "Backend Project Template",
            TemplateSetsRootPath = "EmailTemplates/TemplateSets"
        });
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
                    ["OtpCode"] = "123456",
                    ["Password"] = "P@ssword!123"
                },
                Cc: ["cc-one@test.local", "cc-two@test.local"],
                Bcc: ["bcc-one@test.local"]));

        providerRepository.FirstOrDefaultAsync(Arg.Any<ActiveProviderByTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(Provider.Create(ProviderType.Email, "Mailtrap", "mailtrap", true));
        templateRepository.FirstOrDefaultAsync(Arg.Any<EmailNotificationTemplateByNotificationTypeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(EmailNotificationTemplate.Create(NotificationType.SignInSuccessful, "Sign-in successful notification", "Subject {{:IpAddress:}}", "SignInSuccessful.html"));
        tenantRepository.FirstOrDefaultAsync(Arg.Any<TenantByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(Tenant.Create(tenantId, "Moveaex", "moveaex"));
        transportProvider.ProviderKey.Returns("mailtrap");
        transportProvider.SendAsync(Arg.Any<EmailDeliveryMessage>(), Arg.Any<CancellationToken>())
            .Returns(new EmailTransportSendResult("mailtrap-message-id"));
        hostEnvironment.ContentRootPath.Returns(templateRoot);

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

        var result = await sut.SendAsync(command, CancellationToken.None);

        await logRepository.Received(1).AddAsync(Arg.Any<EmailNotificationLog>(), Arg.Any<CancellationToken>());
        loggedMessageId.ShouldBe(command.MessageId);
        loggedTenantId.ShouldBe(command.TenantId);
        loggedCountryId.ShouldBe(command.CountryId);
        loggedNotificationType.ShouldBe(command.NotificationType);
        loggedTo.ShouldBe(((EmailNotificationContent)command.NotificationContent).To);
        loggedCc.ShouldBe("cc-one@test.local,cc-two@test.local");
        loggedBcc.ShouldBe("bcc-one@test.local");
        loggedEnqueuedAtUtc.ShouldBe(now);
        loggedSentAtUtc.ShouldBeNull();
        loggedProviderMessageId.ShouldBeNull();
        loggedFailureReason.ShouldBeNull();
        loggedNotificationContent["IpAddress"].ShouldBe("127.0.0.1");
        loggedNotificationContent["OtpCode"].ShouldBe("***");
        loggedNotificationContent["Password"].ShouldBe("***");
        loggedNotificationContent.Values.ShouldNotContain("123456");
        loggedNotificationContent.Values.ShouldNotContain("P@ssword!123");
        logRepository.Received(1).Update(Arg.Is<EmailNotificationLog>(log =>
            log.SentAtUtc == now &&
            log.ProviderMessageId == "mailtrap-message-id" &&
            log.FailureReason == null));
        result.ShouldNotBeNull();
        result.ProviderKey.ShouldBe("mailtrap");
        result.ProviderMessageId.ShouldBe("mailtrap-message-id");
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}









