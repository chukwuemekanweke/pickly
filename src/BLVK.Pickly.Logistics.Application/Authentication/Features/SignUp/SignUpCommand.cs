namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SignUp;

using BLVK.Pickly.Logistics.Domain.Common.Auditing;

public sealed record SignUpCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    Guid CountryId,
    string FirstName,
    string LastName,
    string IpAddress,
    ActorContext ActorContext,
    string Language);
