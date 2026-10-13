namespace BLVK.Pickly.Logistics.Consumer.Authentication;

public enum EmailConfirmationOtpSendStatus
{
    Sent = 1,
    ActiveOtpExists = 2,
    InsufficientLifetime = 3
}
