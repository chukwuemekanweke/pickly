using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;

public sealed record GetStakeholderWalletTopUpTransactionDetailCommand(
    Guid WalletTransactionId,
    ActorContext ActorContext);