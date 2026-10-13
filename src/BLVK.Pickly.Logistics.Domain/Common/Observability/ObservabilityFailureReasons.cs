namespace BLVK.Pickly.Logistics.Domain.Common.Observability;

public static class ObservabilityFailureReasons
{
    public const string ActiveOtpExists = "active_otp_exists";
    public const string AlreadyConfirmed = "already_confirmed";
    public const string CountryMismatch = "country_mismatch";
    public const string DuplicateProcessing = "duplicate_processing";
    public const string DuplicateEmail = "duplicate_email";
    public const string DuplicateGoogleAccount = "duplicate_google_account";
    public const string InvalidSignature = "invalid_signature";
    public const string InvalidFile = "invalid_file";
    public const string InvalidGoogleToken = "invalid_google_token";
    public const string InvalidOtp = "invalid_otp";
    public const string InsufficientOtpLifetime = "insufficient_otp_lifetime";
    public const string IncorrectCurrentPassword = "incorrect_current_password";
    public const string NotAuthenticated = "not_authenticated";
    public const string StakeholderNotFound = "stakeholder_not_found";
    public const string TransactionNotFoundOrUnmappedStatus = "transaction_not_found_or_unmapped_status";
    public const string UserNotFound = "user_not_found";
    public const string ValidationFailed = "validation_failed";
}
