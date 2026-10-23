using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.DeviceSignIn;

public sealed record DeviceSignInCommand(
    string DeviceIdentifier,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
