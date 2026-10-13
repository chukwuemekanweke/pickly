using BLVK.Pickly.Logistics.Consumer.Notifications;
using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

public sealed class WhenHandlingEmailNotificationCommandWithInvalidConfiguration_Should
{
    [Fact]
    public async Task ThrowNonTransientException()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var emailNotificationService = Substitute.For<IEmailNotificationService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var messageInboxRepository = Substitute.For<IRepository<MessageInbox>>();
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        var command = new SendNotificationCommand(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.AccountLocked,
            NotificationMedium.Email,
            new EmailNotificationContent(
                ConsumerTestData.Email(),
                new Dictionary<string, string>
                {
                    ["LockedUntilUtc"] = "2026-04-11T00:00:00.0000000+00:00"
                }));

        emailNotificationService
            .SendAsync(command, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<EmailNotificationSendResult?>(new NotificationConfigurationException("No email provider is configured.")));

        var exception = await Should.ThrowAsync<CannotProcessMessageNonTransientException>(() =>
            new SendNotificationHandler(
                    customTelemetryContext,
                    currentActorAccessor,
                    messageContext,
                    emailNotificationService,
                    unitOfWork,
                    TimeProvider.System,
                    messageInboxRepository,
                    Substitute.For<ILogger<SendNotificationHandler>>())
                .HandleAsync(command, CancellationToken.None));

        exception.Message.ShouldBe("No email provider is configured.");
    }
}

