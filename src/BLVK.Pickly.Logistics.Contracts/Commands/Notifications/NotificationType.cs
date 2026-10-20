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
    SubscriptionInvoice = 10,
    OrderCreated = 11,
    PaymentInitiated = 12,
    RiderAssigned = 13,
    PaymentFailed = 14,
    PaymentSucceeded = 15,
    RiderAcceptedInstructions = 16

}
