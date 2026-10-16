namespace BLVK.Pickly.Logistics.Domain.Common.Observability;

public static class Observability
{
    public const string ActivitySourceName = "BLVK.Pickly.Logistics";
    public const string MeterName = "BLVK.Pickly.Logistics";
    public const string FlowIdHeaderName = "X-Flow-Id";

    public static class PropertyNames
    {
        public static class Common
        {
            public const string MessageType = "message_type";
            public const string MessageId = "message_id";
            public const string OccurredAt = "occurred_at";
            public const string RequestedAt = "requested_at";
            public const string UserId = "user_id";
            public const string StakeholderId = "stakeholder_id";
            public const string TenantId = "tenant_id";
            public const string CorrelationId = "correlation_id";
            public const string FlowId = "flow.id";
            public const string FailureReason = "failure_reason";
            public const string ExceptionType = "exception_type";
            public const string UploadId = "upload_id";
            public const string ContentType = "content_type";
            public const string ContentLength = "content_length";
            public const string ExpiresAtUtc = "expires_at_utc";
            public const string QueueDelayMilliseconds = "queue_delay_ms";
            public const string RemainingLifetimeMilliseconds = "remaining_lifetime_ms";
        }

        public static class Notifications
        {
            public const string ProviderKey = "provider_key";
            public const string ProviderMessageId = "provider_message_id";
            public const string NotificationType = "notification_type";
        }

        public static class Payments
        {
            public const string Provider = "provider";
            public const string PaymentReference = "payment_reference";
            public const string MerchantReference = "merchant_reference";
            public const string ProviderReference = "provider_reference";
            public const string PaymentMethod = "payment_method";
            public const string PaymentIntent = "payment_intent";
            public const string CurrencyId = "currency_id";
            public const string CurrencyCode = "currency_code";
            public const string WalletId = "wallet_id";
            public const string TerminalState = "terminal_state";
        }

        public static class Locations
        {
            public const string LocationId = "location_id";
            public const string Provider = "geocoding_provider";
            public const string Outcome = "resolution_outcome";
            public const string MatchKind = "registry_match_kind";
            public const string CandidateCount = "candidate_count";
            public const string CountryCode = "country_code";
            public const string DurationMilliseconds = "duration_ms";
        }
    }

    public static class EventNames
    {
        public static class Authentication
        {
            public const string PasswordSignUpStarted = "PasswordSignUpStarted";
            public const string PasswordSignUpCompleted = "PasswordSignUpCompleted";
            public const string PasswordSignUpFailed = "PasswordSignUpFailed";
            public const string GoogleSignUpStarted = "GoogleSignUpStarted";
            public const string GoogleSignUpCompleted = "GoogleSignUpCompleted";
            public const string GoogleSignUpFailed = "GoogleSignUpFailed";
            public const string EmailConfirmationOtpSent = "EmailConfirmationOtpSent";
            public const string EmailConfirmationOtpSendSkipped = "EmailConfirmationOtpSendSkipped";
            public const string EmailConfirmationStarted = "EmailConfirmationStarted";
            public const string EmailConfirmationCompleted = "EmailConfirmationCompleted";
            public const string EmailConfirmationFailed = "EmailConfirmationFailed";
            public const string PasswordSignInStarted = "PasswordSignInStarted";
            public const string PasswordSignInCompleted = "PasswordSignInCompleted";
            public const string GoogleSignInStarted = "GoogleSignInStarted";
            public const string GoogleSignInCompleted = "GoogleSignInCompleted";
            public const string SignInPostProcessingCompleted = "SignInPostProcessingCompleted";
            public const string SignInFailureProcessed = "SignInFailureProcessed";
            public const string PasswordResetRequested = "PasswordResetRequested";
            public const string PasswordResetRequestFailed = "PasswordResetRequestFailed";
            public const string PasswordResetOtpSent = "PasswordResetOtpSent";
            public const string PasswordResetCompleted = "PasswordResetCompleted";
            public const string PasswordResetCompletionFailed = "PasswordResetCompletionFailed";
            public const string PasswordChangeCompleted = "PasswordChangeCompleted";
            public const string PasswordChangeFailed = "PasswordChangeFailed";
            public const string SignOutCompleted = "SignOutCompleted";
            public const string SessionRefreshCompleted = "SessionRefreshCompleted";
            public const string SessionRefreshPostProcessingCompleted = "SessionRefreshPostProcessingCompleted";
            public const string TwoFactorChallengeRequired = "TwoFactorChallengeRequired";
            public const string TwoFactorVerificationCompleted = "TwoFactorVerificationCompleted";
            public const string TwoFactorVerificationFailed = "TwoFactorVerificationFailed";
            public const string TwoFactorEnrollmentCompleted = "TwoFactorEnrollmentCompleted";
            public const string TwoFactorDisabled = "TwoFactorDisabled";
            public const string TwoFactorRecoveryCodesRegenerated = "TwoFactorRecoveryCodesRegenerated";
            public const string ProfileUpdateCompleted = "ProfileUpdateCompleted";
            public const string ProfileUpdateFailed = "ProfileUpdateFailed";
            public const string AvatarUploadCompleted = "AvatarUploadCompleted";
            public const string AvatarUploadFailed = "AvatarUploadFailed";
            public const string AvatarUploadInitiated = "AvatarUploadInitiated";
        }

        public static class Notifications
        {
            public const string EmailSent = "EmailNotificationSent";
            public const string EmailDelivered = "EmailNotificationDelivered";
        }

        public static class Payments
        {
            public const string Initiated = "payment.initiated";
            public const string WebhookReceived = "payment.webhook.received";
            public const string WebhookPersisted = "payment.webhook.persisted";
            public const string WebhookPersistenceFailed = "payment.webhook.persistence_failed";
            public const string ReconciliationConfirmed = "payment.reconciliation.confirmed";
            public const string ReconciliationFailed = "payment.reconciliation.failed";
            public const string SubscriptionActivated = "payment.subscription.activated";
            public const string WalletCreated = "payment.wallet.created";
            public const string WalletCredited = "payment.wallet.credited";
            public const string CreditWallet = "payment.credit_wallet";
            public const string ActivateSubscription = "payment.activate_subscription";
        }

        public static class Locations
        {
            public const string RegistryHit = "location.registry.hit";
            public const string RegistryMiss = "location.registry.miss";
            public const string GeocodingRequested = "location.geocoding.requested";
            public const string GeocodingFailed = "location.geocoding.failed";
            public const string ResolutionAmbiguous = "location.resolution.ambiguous";
            public const string ResolutionFailed = "location.resolution.failed";
            public const string Registered = "location.registered";
            public const string Reused = "location.reused";
            public const string DuplicateResolutionPrevented = "location.resolution.duplicate_prevented";
            public const string CandidateSelected = "location.candidate.selected";
        }
    }
}
