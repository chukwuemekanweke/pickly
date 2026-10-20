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
}
