namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RequestEmailConfirmationOtp;

public sealed record RequestEmailConfirmationOtpResponse(string Message, DateTimeOffset RetryAtUtc);
