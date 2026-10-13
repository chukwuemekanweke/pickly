namespace BLVK.Pickly.Logistics.Contracts.Commands.Notifications;

public enum NotificationType
{
    AccountCreated = 1,
    EmailConfirmationOtp = 2,
    ResetPasswordOtp = 3,
    PasswordResetSuccessful = 4,
    EmailConfirmationFollowUp = 5,
    SignInSuccessful = 6,
    AccountLocked = 7,
    TrialExpired = 8,
    SubscriptionCancelled = 9,
    SubscriptionInvoice = 10

}
