namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SignUp;

public sealed record SignUpResponse(string Email, string Message, DateTimeOffset RetryAtUtc);
