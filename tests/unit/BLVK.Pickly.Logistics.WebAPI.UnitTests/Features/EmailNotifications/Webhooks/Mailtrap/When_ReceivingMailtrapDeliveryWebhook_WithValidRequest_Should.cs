using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common;
using BLVK.Pickly.Logistics.Domain.Notifications.Services;
using BLVK.Pickly.Logistics.Domain.Notifications.Specifications;
using BLVK.Pickly.Logistics.WebAPI.Features.EmailNotifications.Webhooks.Mailtrap;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System.Text;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.EmailNotifications.Webhooks.Mailtrap;

public sealed class When_ReceivingMailtrapDeliveryWebhook_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnOk()
    {
        var context = new EmailNotificationsControllerTestContext();
        var provider = context.CreateMailtrapProvider();
        BLVK.Pickly.Logistics.Domain.Notifications.Entities.EmailDeliveryWebhookInbox? capturedInbox = null;

        context.ProviderRepository.FirstOrDefaultAsync(Arg.Any<ProviderByTypeAndKeySpecification>(), Arg.Any<CancellationToken>())
            .Returns(provider);
        context.MailtrapWebhookSignatureValidator.ValidateAsync(Arg.Any<MailtrapWebhookSignatureValidationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MailtrapWebhookSignatureValidationResult(true, KnownWebhookStatusChangeReasons.Shared.SignatureVerified));
        context.EmailDeliveryWebhookInboxRepository.ListAsync(Arg.Any<EmailDeliveryWebhookInboxesByEventIdsSpecification>(), Arg.Any<CancellationToken>())
            .Returns([]);
        context.EmailDeliveryWebhookInboxRepository.AddAsync(
                Arg.Do<BLVK.Pickly.Logistics.Domain.Notifications.Entities.EmailDeliveryWebhookInbox>(inbox => capturedInbox = inbox),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        const string payload = "{\"events\":[{\"event\":\"delivery\",\"message_id\":\"mailtrap-message-id\",\"sending_stream\":\"transactional\",\"email\":\"ada@example.com\",\"sending_domain_name\":\"mail.example.com\",\"timestamp\":1746273900,\"event_id\":\"evt_123\"}]}";
        var sut = new MailtrapWebhooksController(context.CreateHandler(), NullLogger<MailtrapWebhooksController>.Instance);
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        sut.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        sut.ControllerContext.HttpContext.Request.Headers["Mailtrap-Signature"] = "valid-signature";

        var result = await sut.Handle(CancellationToken.None);

        result.ShouldBeOfType<OkResult>();
        capturedInbox.ShouldNotBeNull();
        capturedInbox.WebhookProcessingStatus.ShouldBe(WebhookProcessingStatus.Received);
        await context.MailtrapWebhookSignatureValidator.Received(1).ValidateAsync(
            Arg.Is<MailtrapWebhookSignatureValidationRequest>(request =>
                request.SignatureHeader == "valid-signature" &&
                request.RawPayload == payload),
            Arg.Any<CancellationToken>());
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<EmailDeliveryWebhookReceived>(message =>
                message.ProviderId == provider.Id &&
                message.EventId == "evt_123" &&
                message.ProviderMessageId == "mailtrap-message-id"),
            Arg.Any<CancellationToken>());
    }
}
