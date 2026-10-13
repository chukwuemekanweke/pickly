namespace BLVK.Pickly.Logistics.Application.Payments.Features.ProcessCredoWebhook;

public static class CredoWebhookEvents
{
    public const string TransactionSuccessful = "transaction.successful";
    public const string TransactionFailed = "transaction.failed";
    public const string TransactionTransferReverse = "transaction.transaction.transfer.reverse";
}
