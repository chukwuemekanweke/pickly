using BLVK.Pickly.Logistics.Domain.Common;
using BLVK.Pickly.Logistics.Domain.Notifications.Services;
using BLVK.Pickly.Logistics.WebAPI.Features.EmailNotifications.Webhooks.Mailtrap;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shouldly;
using System.Text;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.EmailNotifications.Webhooks.Mailtrap;

public sealed class When_ReceivingMailtrapDeliveryWebhook_WithInvalidSignature_Should
{
    [Fact]
    public async Task ReturnUnauthorized()
    {
        var context = new EmailNotificationsControllerTestContext();
        var logger = new CapturingLogger<MailtrapWebhooksController>();

        context.MailtrapWebhookSignatureValidator.ValidateAsync(Arg.Any<MailtrapWebhookSignatureValidationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MailtrapWebhookSignatureValidationResult(false, KnownWebhookStatusChangeReasons.Shared.InvalidSignature));

        const string payload = "{\"events\":[{\"event\":\"delivery\",\"message_id\":\"mailtrap-message-id\",\"sending_stream\":\"transactional\",\"email\":\"ada@example.com\",\"sending_domain_name\":\"mail.example.com\",\"timestamp\":1746273900,\"event_id\":\"evt_123\"}]}";
        var sut = new MailtrapWebhooksController(context.CreateHandler(), logger);
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        sut.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        sut.ControllerContext.HttpContext.Request.Headers["Mailtrap-Signature"] = "invalid-signature";

        var result = await sut.Handle(CancellationToken.None);

        result.ShouldBeOfType<UnauthorizedObjectResult>();
        logger.Messages.Count.ShouldBe(1);
        logger.Messages.Single().ShouldContain(KnownWebhookStatusChangeReasons.Shared.InvalidSignature);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
