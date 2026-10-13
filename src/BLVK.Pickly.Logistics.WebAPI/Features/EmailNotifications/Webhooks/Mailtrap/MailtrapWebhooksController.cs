using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.WebAPI.Features.EmailNotifications.Webhooks.Mailtrap;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route(EndpointUrl.EmailNotificationWebhooks.Mailtrap.Route)]
public sealed class MailtrapWebhooksController(
    ProcessMailtrapDeliveryWebhookHandler handler,
    ILogger<MailtrapWebhooksController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        var rawPayload = await ReadRawPayloadAsync(cancellationToken);
        var request = JsonSerializer.Deserialize<MailtrapDeliveryWebhookRequest>(rawPayload, JsonSerializerOptions)
            ?? throw new JsonException("Unable to deserialize Mailtrap delivery webhook payload.");

        if (request.Events.Length == 0)
        {
            return BadRequest("At least one delivery event is required.");
        }

        if (request.Events.Any(item => !string.Equals(item.Event, MailtrapDeliveryWebhookEvents.Delivery, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Unsupported Mailtrap webhook event.");
        }

        var result = await handler.HandleAsync(
            new ProcessMailtrapDeliveryWebhookCommand(
                request.Events
                    .Select(item => new MailtrapDeliveryWebhookEvent(
                        item.Event,
                        item.MessageId,
                        item.SendingStream,
                        item.Email,
                        item.SendingDomainName,
                        item.Timestamp,
                        item.EventId))
                    .ToArray(),
                rawPayload,
                Request.Headers["Mailtrap-Signature"]),
            cancellationToken);

        if (result.Status == MailtrapDeliveryWebhookReceiptStatus.InvalidSignature)
        {
            logger.LogWarning(
                "Mailtrap webhook signature validation failed. Reason: {Reason}",
                result.StatusChangeReason ?? "unknown");
            return Unauthorized("Invalid signature");
        }

        return Ok();
    }

    private async Task<string> ReadRawPayloadAsync(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawPayload = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        return rawPayload;
    }
}
