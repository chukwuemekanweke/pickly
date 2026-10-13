using BLVK.Pickly.Logistics.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using BLVK.Pickly.Logistics.Domain.Notifications.Services;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Notifications;

internal sealed class NotificationsFlowTestContext
{
    public IReadRepository<Provider> ProviderRepository { get; } = Substitute.For<IReadRepository<Provider>>();
    public IRepository<EmailDeliveryWebhookInbox> EmailDeliveryWebhookInboxRepository { get; } = Substitute.For<IRepository<EmailDeliveryWebhookInbox>>();
    public IMailtrapWebhookSignatureValidator MailtrapWebhookSignatureValidator { get; } = Substitute.For<IMailtrapWebhookSignatureValidator>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero));

    public ProcessMailtrapDeliveryWebhookHandler CreateHandler() =>
        new(
            ProviderRepository,
            EmailDeliveryWebhookInboxRepository,
            MailtrapWebhookSignatureValidator,
            EventPublisher,
            UnitOfWork,
            Clock);

    public Provider CreateMailtrapProvider() =>
        Provider.Create(ProviderType.Email, "Mailtrap", "mailtrap", true);

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

