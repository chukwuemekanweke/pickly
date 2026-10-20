using BLVK.Pickly.Logistics.Domain.Common.Notifications;
using BLVK.Pickly.Logistics.Domain.Deliveries;
using BLVK.Pickly.Logistics.Domain.Notifications.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BLVK.Pickly.Logistics.Infrastructure.Notifications;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeliveryNotificationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(DeliveryNotificationOptions.SectionName)
            .Get<DeliveryNotificationOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{DeliveryNotificationOptions.SectionName}' is required.");
        options.Validate();
        services.Configure<DeliveryNotificationOptions>(
            configuration.GetSection(DeliveryNotificationOptions.SectionName));
        return services;
    }

    public static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration
            .GetSection(EmailNotificationsOptions.SectionName)
            .Get<EmailNotificationsOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{EmailNotificationsOptions.SectionName}' is required.");

        options.Validate();

        services.Configure<EmailNotificationsOptions>(configuration.GetSection(EmailNotificationsOptions.SectionName));
        services.AddHttpClient(NotificationHttpClientNames.Mailtrap);
        services.AddScoped<IEmailNotificationService, EmailNotificationDispatcher>();
        services.AddScoped<IEmailTransportProvider, MailtrapEmailTransportProvider>();

        return services;
    }

    public static IServiceCollection AddNotificationWebhookServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailNotificationsOptions>(configuration.GetSection(EmailNotificationsOptions.SectionName));
        services.AddScoped<IMailtrapWebhookSignatureValidator, MailtrapWebhookSignatureValidator>();

        return services;
    }
}
