using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Formatting;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;

namespace BLVK.Pickly.Logistics.Consumer.Authentication;

public sealed class EmailConfirmationOtpSender(
    ITwoFactorOtpService twoFactorOtpService,
    ICommandSender commandSender,
    IRegistrationPolicyProvider registrationPolicyProvider,
    TimeProvider timeProvider)
{
    public async Task<EmailConfirmationOtpSendStatus> SendAsync(
        StakeholderReadModel stakeholder,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken)
    {
        if (await twoFactorOtpService.OtpExistsAsync(
                stakeholder.AppUserId,
                OtpIntent.EmailConfirmation,
                cancellationToken))
        {
            return EmailConfirmationOtpSendStatus.ActiveOtpExists;
        }

        var remainingLifetime = expiresAtUtc - timeProvider.GetUtcNow();
        if (remainingLifetime <= AuthenticationOtpDefaults.EmailConfirmationDeliverySafetyThreshold)
        {
            return EmailConfirmationOtpSendStatus.InsufficientLifetime;
        }

        var registrationPolicy = await registrationPolicyProvider.GetAsync();
        var otp = await twoFactorOtpService.GenerateOtpAsync(
            stakeholder.AppUserId,
            OtpIntent.EmailConfirmation,
            cancellationToken,
            characterLength: registrationPolicy.EmailConfirmationOtpLength,
            isAlphaNumeric: false,
            expiresAtUtc: expiresAtUtc);

        await commandSender.SendAsync(
            new SendNotificationCommand(
                stakeholder.TenantId,
                stakeholder.CountryId,
                NotificationType.EmailConfirmationOtp,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    stakeholder.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = stakeholder.FirstName,
                        ["LastName"] = stakeholder.LastName,
                        ["OtpCode"] = otp.Code,
                        ["OtpExpiresAtUtc"] = DateTimeFormatter.FormatHumanReadableUtc(
                            otp.ExpiresAtUtc,
                            timeProvider.GetUtcNow()),
                        ["VerifyUrl"] = string.Empty,
                        ["Product"] = "BLVK.Pickly.Logistics"
                    }))
            {
                StakeholderId = stakeholder.StakeholderId,
                Language = stakeholder.Language
            },
            cancellationToken);

        return EmailConfirmationOtpSendStatus.Sent;
    }
}
