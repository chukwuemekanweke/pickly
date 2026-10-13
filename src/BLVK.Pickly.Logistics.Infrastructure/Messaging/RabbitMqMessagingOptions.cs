namespace BLVK.Pickly.Logistics.Infrastructure.Messaging;

public sealed class RabbitMqMessagingOptions
{
    public const string SectionName = "Messaging:RabbitMq";

    public string ServiceName { get; init; } = string.Empty;
    public string HostName { get; init; } = string.Empty;
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string VirtualHost { get; init; } = "/";
    public string EventsExchange { get; init; } = string.Empty;
    public string CommandsExchange { get; init; } = string.Empty;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ServiceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(HostName);
        ArgumentException.ThrowIfNullOrWhiteSpace(UserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(Password);
        ArgumentException.ThrowIfNullOrWhiteSpace(VirtualHost);
        ArgumentException.ThrowIfNullOrWhiteSpace(EventsExchange);
        ArgumentException.ThrowIfNullOrWhiteSpace(CommandsExchange);

        if (Port <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Port), "RabbitMQ port must be greater than zero.");
        }
    }
}
