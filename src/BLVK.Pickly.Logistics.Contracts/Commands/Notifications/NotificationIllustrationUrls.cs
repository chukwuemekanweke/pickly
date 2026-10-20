namespace BLVK.Pickly.Logistics.Contracts.Commands.Notifications;

public static class NotificationIllustrationUrls
{
    private const string AssetRoot =
        "https://pub-0c671c99962543c1a440d1d9f1ce3a90.r2.dev/BLVK.Pickly.Logistics/email/transactional/v1";

    public const string OrderCreated = $"{AssetRoot}/order-created.png";
    public const string PaymentInitiated = $"{AssetRoot}/payment-initiated.png";
    public const string RiderAssigned = $"{AssetRoot}/rider-assigned.png";
    public const string PaymentFailed = $"{AssetRoot}/payment-failed.png";
    public const string PaymentSucceeded = $"{AssetRoot}/payment-succeeded.png";
    public const string RiderInstructions = $"{AssetRoot}/rider-instructions.png";
    public const string AccountCreated = $"{AssetRoot}/account-created.png";
    public const string AccountLocked = $"{AssetRoot}/account-locked.png";
    public const string CancelledSubscription = $"{AssetRoot}/cancelled-subscription.png";
    public const string ConfirmEmail = $"{AssetRoot}/confirm-email.png";
    public const string EmailConfirmationFollowUp = $"{AssetRoot}/email-confirmation-follow-up.png";
    public const string Invoice = $"{AssetRoot}/invoice.png";
    public const string PasswordResetSuccessful = $"{AssetRoot}/password-reset-successful.png";
    public const string ResetPassword = $"{AssetRoot}/reset-password.png";
    public const string SignInSuccessful = $"{AssetRoot}/sign-in-successful.png";
    public const string TrialExpired = $"{AssetRoot}/trial-expired.png";
}
