namespace BLVK.Pickly.Logistics.Domain.Payments;

public static class KnownPaymentTransactionChangeReasons
{
    public const string PaymentInitiated = "payment_initiated";
    public const string ReconciliationConfirmedSuccess = "reconciliation_confirmed_success";
    public const string ReconciliationConfirmedFailure = "reconciliation_confirmed_failure";
    public const string ReconciliationConfirmedCustomerCancellation = "reconciliation_confirmed_customer_cancellation";
    public const string ReconciliationConfirmedMerchantCancellation = "reconciliation_confirmed_merchant_cancellation";
    public const string ReconciliationConfirmedRefund = "reconciliation_confirmed_refund";
    public const string ReconciliationConfirmedRefundQueued = "reconciliation_confirmed_refund_queued";
    public const string ReconciliationConfirmedExpired = "reconciliation_confirmed_expired";
    public const string ReconciliationStillProcessing = "reconciliation_still_processing";
    public const string ReconciliationUnresolvedSettlement = "reconciliation_unresolved_settlement";
}
