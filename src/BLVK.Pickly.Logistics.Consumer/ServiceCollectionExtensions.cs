using BLVK.Pickly.Logistics.Consumer.Authentication;
using BLVK.Pickly.Logistics.Consumer.Notifications;
using BLVK.Pickly.Logistics.Consumer.Payments;
using BLVK.Pickly.Logistics.Consumer.Storage;
using BLVK.Pickly.Logistics.Contracts.Commands.Payments;
using BLVK.Pickly.Logistics.Contracts.Commands.Storage;
using BLVK.Pickly.Logistics.Infrastructure.Messaging;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer.DependencyInjection;
using EmailDeliveryWebhookReceivedEvent = BLVK.Pickly.Logistics.Contracts.Events.EmailDeliveryWebhookReceived;
using ResetPassword = BLVK.Pickly.Logistics.Contracts.Commands.Authentication.ResetPasswordCommand;
using SendEmailConfirmationOtp = BLVK.Pickly.Logistics.Contracts.Commands.Authentication.SendEmailConfirmationOtpCommand;
using SendNotification = BLVK.Pickly.Logistics.Contracts.Commands.Notifications.SendNotificationCommand;
using SuccessfulPaymentConfirmedEvent = BLVK.Pickly.Logistics.Contracts.Events.SuccessfulPaymentConfirmed;
using UserAccessTokenRefreshedEvent = BLVK.Pickly.Logistics.Contracts.Events.UserAccessTokenRefreshed;
using UserCreatedEvent = BLVK.Pickly.Logistics.Contracts.Events.UserCreated;
using UserEmailConfirmedEvent = BLVK.Pickly.Logistics.Contracts.Events.UserEmailConfirmed;
using UserSignInFailedEvent = BLVK.Pickly.Logistics.Contracts.Events.UserSignInFailed;
using UserSignInSuccessfulEvent = BLVK.Pickly.Logistics.Contracts.Events.UserSignInSuccessful;

namespace BLVK.Pickly.Logistics.Consumer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSubscribers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ILoginActivityIpAddressResolver, LoginActivityIpAddressResolver>();
        services.AddScoped<EmailConfirmationOtpSender>();

        var options = configuration
            .GetSection(RabbitMqMessagingOptions.SectionName)
            .Get<RabbitMqMessagingOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{RabbitMqMessagingOptions.SectionName}' is required.");

        options.Validate();

        var subscriberConfig = new SubscriberConfig
        {
            ServiceName = options.ServiceName,
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            SubscriptionName = "authentication-events",
            ExchangeName = options.EventsExchange,
            PrefetchCount = 5,
            MaxRetryCount = 10,
            ConcurrentMessageCount = 1
        };

        var consumerConfig = new ConsumerConfig
        {
            ServiceName = options.ServiceName,
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            QueueName = "notifications",
            ExchangeName = options.CommandsExchange,
            PrefetchCount = 5,
            MaxRetryCount = 10,
            ConcurrentMessageCount = 1
        };

        services
            .AddSubscriber(subscriberConfig, builder => builder
                .AddHandler<UserCreatedEvent, UserCreatedHandler>()
                .AddHandler<UserEmailConfirmedEvent, UserEmailConfirmedHandler>()
                .AddHandler<UserSignInSuccessfulEvent, UserSignInSuccessfulHandler>()
                .AddHandler<UserAccessTokenRefreshedEvent, UserAccessTokenRefreshedHandler>()
                .AddHandler<UserSignInFailedEvent, UserSignInFailedHandler>()
                .AddHandler<EmailDeliveryWebhookReceivedEvent, EmailDeliveryWebhookReceivedHandler>()
                .AddHandler<SuccessfulPaymentConfirmedEvent, SuccessfulPaymentConfirmedHandler>())
            .AddConsumer(consumerConfig, builder => builder
                .AddHandler<ResetPassword, ResetPasswordHandler>()
                .AddHandler<SendEmailConfirmationOtp, SendEmailConfirmationOtpHandler>()
                .AddHandler<SendNotification, SendNotificationHandler>()
                .AddHandler<CreditWalletCommand, CreditWalletHandler>()
                .AddHandler<ActivateSubscriptionCommand, ActivateSubscriptionHandler>()
                .AddHandler<DeleteQuarantinedObject, DeleteQuarantinedObjectHandler>())
            .AddHostedService(serviceProvider => new Worker(
                serviceProvider.GetRequiredKeyedService<ISubscriber>(subscriberConfig.Key),
                serviceProvider.GetRequiredKeyedService<IConsumer>(consumerConfig.Key),
                serviceProvider.GetRequiredService<ILogger<Worker>>(),
                serviceProvider.GetRequiredService<WorkerReadinessState>(),
                serviceProvider.GetRequiredService<TimeProvider>()));

        return services;
    }
}
