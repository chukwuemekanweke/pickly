namespace BLVK.Pickly.Logistics.Contracts.Events;

public sealed record UserSignInSuccessful(
    string IpAddress,
    string UserAgent) : BaseEvent;
