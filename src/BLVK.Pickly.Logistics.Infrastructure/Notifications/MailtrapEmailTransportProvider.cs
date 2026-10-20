using BLVK.Pickly.Logistics.Domain.Notifications;
using Mailtrap;
using Mailtrap.Configuration;
using Mailtrap.Emails;
using Mailtrap.Emails.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace BLVK.Pickly.Logistics.Infrastructure.Notifications;

internal sealed class MailtrapEmailTransportProvider(
    IOptions<EmailNotificationsOptions> options,
    IHttpClientFactory httpClientFactory,
    ILogger<MailtrapEmailTransportProvider> logger) : IEmailTransportProvider
{
    private static readonly ResiliencePipeline RetryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder().Handle<Exception>()
        })
        .Build();

    public string ProviderKey => EmailProviderKeys.Mailtrap;

    public async Task<EmailTransportSendResult> SendAsync(EmailDeliveryMessage message, CancellationToken cancellationToken = default)
    {
        var mailtrapOptions = options.Value.Mailtrap;
        mailtrapOptions.Validate();

        using var httpClient = httpClientFactory.CreateClient(NotificationHttpClientNames.Mailtrap);
        using var factory = new MailtrapClientFactory(new MailtrapClientOptions(mailtrapOptions.ApiToken), httpClient);
        var client = factory.CreateClient();
        var request = CreateRequest(message);
        var emailClient = ResolveEmailClient(client, mailtrapOptions);
        string? providerMessageId = null;

        await RetryPipeline.ExecuteAsync(
            async token =>
            {
                var response = await emailClient.Send(request, token);
                providerMessageId = response.MessageIds.FirstOrDefault();
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(providerMessageId))
        {
            logger.LogError("Mailtrap did not return a provider message ID for email sent to {Recipient}.", message.To);
            throw new InvalidOperationException("Mailtrap did not return a provider message ID.");
        }

        return new EmailTransportSendResult(providerMessageId);
    }

    internal static SendEmailRequest CreateRequest(EmailDeliveryMessage message)
    {
        var request = SendEmailRequest
            .Create()
            .From(message.FromAddress, message.FromName)
            .To(message.To)
            .Subject(message.Subject)
            .Html(message.HtmlBody);

        if (message.Cc is not null)
        {
            foreach (var cc in message.Cc)
            {
                request.Cc(cc);
            }
        }

        if (message.Bcc is not null)
        {
            foreach (var bcc in message.Bcc)
            {
                request.Bcc(bcc);
            }
        }

        return request;
    }

    private static ISendEmailClient ResolveEmailClient(
        IMailtrapClient client,
        EmailNotificationsOptions.MailtrapOptions options)
    {
        if (options.InboxId.HasValue)
        {
            return client.Test(options.InboxId.Value);
        }

        return options.UseBulkApi ? client.Bulk() : client.Email();
    }

}
