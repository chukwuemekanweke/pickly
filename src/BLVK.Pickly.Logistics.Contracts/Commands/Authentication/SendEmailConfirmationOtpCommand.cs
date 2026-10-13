namespace BLVK.Pickly.Logistics.Contracts.Commands.Authentication;

public sealed record SendEmailConfirmationOtpCommand : BaseCommand
{
    public DateTimeOffset ExpiresAtUtc { get; init; }
}
