namespace BLVK.Pickly.Logistics.Contracts.Events;

public sealed record UserCreated : BaseEvent
{
    public DateTimeOffset RequestedAtUtc { get; init; }

    public DateTimeOffset ExpiresAtUtc { get; init; }
}
