using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTransactions;

public sealed record GetStakeholderWalletTransactionsCommand(
    int Limit,
    string? Cursor,
    ActorContext ActorContext);
