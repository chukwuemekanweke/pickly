namespace BLVK.Pickly.Logistics.Domain.Common;

public static class KnownWebhookStatusChangeReasons
{
    public static class Shared
    {
        public const string SignatureVerified = "signature_verified";
        public const string InvalidSignature = "invalid_signature";
        public const string MissingSignature = "missing_signature";
    }

    public static class Payments
    {
        public const string SignatureNotApplicable = "signature_not_applicable";
        public const string MissingSecretKey = "missing_secret_key";
        public const string MissingBusinessCode = "missing_business_code";
        public const string TransactionNotFoundOrUnmappedStatus = "transaction_not_found_or_unmapped_status";
    }

    public static class Notifications
    {
        public const string MissingSigningSecret = "missing_signing_secret";
        public const string MissingPayload = "missing_payload";
        public const string NotificationLogDelivered = "notification_log_delivered";
    }
}
