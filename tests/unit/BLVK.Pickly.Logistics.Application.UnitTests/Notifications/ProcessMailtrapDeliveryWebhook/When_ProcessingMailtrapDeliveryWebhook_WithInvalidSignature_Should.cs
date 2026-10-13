using BLVK.Pickly.Logistics.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;
using BLVK.Pickly.Logistics.Domain.Common;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using BLVK.Pickly.Logistics.Domain.Notifications.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Notifications.ProcessMailtrapDeliveryWebhook;

public sealed class When_ProcessingMailtrapDeliveryWebhook_WithInvalidSignature_Should
{
    [Fact]
    public async Task ReturnInvalidSignature()
    {
        var context = new NotificationsFlowTestContext();

        context.MailtrapWebhookSignatureValidator.ValidateAsync(Arg.Any<MailtrapWebhookSignatureValidationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MailtrapWebhookSignatureValidationResult(false, KnownWebhookStatusChangeReasons.Shared.InvalidSignature));

        var result = await context.CreateHandler().HandleAsync(
            new ProcessMailtrapDeliveryWebhookCommand(
                [
                    new MailtrapDeliveryWebhookEvent(
                        MailtrapDeliveryWebhookEvents.Delivery,
                        "mailtrap-message-id",
                        "transactional",
                        "ada@example.com",
                        "mail.example.com",
                        1,
                        "evt_123")
                ],
                "{\"events\":[]}",
                "invalid-signature"),
            CancellationToken.None);

        result.Status.ShouldBe(MailtrapDeliveryWebhookReceiptStatus.InvalidSignature);
        result.StatusChangeReason.ShouldBe(KnownWebhookStatusChangeReasons.Shared.InvalidSignature);
        await context.EmailDeliveryWebhookInboxRepository.DidNotReceive().AddAsync(Arg.Any<EmailDeliveryWebhookInbox>(), Arg.Any<CancellationToken>());
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
