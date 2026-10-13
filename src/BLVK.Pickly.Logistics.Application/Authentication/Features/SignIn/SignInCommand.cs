namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;

using BLVK.Pickly.Logistics.Domain.Common.Auditing;

public sealed record SignInCommand(
    string Email,
    string Password,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
