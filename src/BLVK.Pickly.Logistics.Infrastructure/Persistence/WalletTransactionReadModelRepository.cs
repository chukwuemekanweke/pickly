using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class WalletTransactionReadModelRepository(AppReadDbContext dbContext) : IWalletTransactionReadModelRepository
{
    public async Task<StakeholderWalletTransactionsCursorPage> GetByStakeholderAsync(
        StakeholderWalletTransactionsCursorRequest request,
        CancellationToken cancellationToken)
    {
        var query =
            from walletTransaction in dbContext.WalletTransactions.AsNoTracking()
            join wallet in dbContext.Wallets.AsNoTracking() 
                on walletTransaction.WalletId equals wallet.Id
            join currency in dbContext.Currencies.AsNoTracking() 
                on walletTransaction.CurrencyId equals currency.Id
            where wallet.StakeholderId == request.StakeholderId
            select new
            {
                walletTransaction.Id,
                walletTransaction.TransactionTitle,
                walletTransaction.Amount,
                currency.CurrencyCode,
                walletTransaction.TransactionType,
                walletTransaction.TransactionCategory,
                walletTransaction.CreatedAtUtc
            };

        if (request.CursorCreatedAtUtc.HasValue && request.CursorTransactionId.HasValue)
        {
            var cursorCreatedAtUtc = request.CursorCreatedAtUtc.Value;
            var cursorTransactionId = request.CursorTransactionId.Value;

            query = query.Where(item =>
                item.CreatedAtUtc < cursorCreatedAtUtc ||
                (item.CreatedAtUtc == cursorCreatedAtUtc && item.Id.CompareTo(cursorTransactionId) < 0));
        }

        var items = await query
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.Id)
            .Take(request.Limit + 1)
            .Select(item => new StakeholderWalletTransactionReadModel(
                item.Id,
                item.TransactionTitle,
                item.Amount,
                item.CurrencyCode,
                item.TransactionType,
                item.TransactionCategory,
                item.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > request.Limit;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        return new StakeholderWalletTransactionsCursorPage(items, hasMore);
    }

    public async Task<StakeholderWalletTopUpTransactionDetailReadModel?> GetWalletTopUpDetailByStakeholderAsync(
        StakeholderWalletTopUpTransactionDetailRequest request,
        CancellationToken cancellationToken)
    {
        return await (
            from walletTransaction in dbContext.WalletTransactions.AsNoTracking()
            join wallet in dbContext.Wallets.AsNoTracking() 
                on walletTransaction.WalletId equals wallet.Id
            join currency in dbContext.Currencies.AsNoTracking() 
                on walletTransaction.CurrencyId equals currency.Id
            join paymentTransaction in dbContext.PaymentTransactions.AsNoTracking() 
                on walletTransaction.PaymentTransactionId equals paymentTransaction.Id
            join paymentProvider in dbContext.PaymentProviders.AsNoTracking() 
                on paymentTransaction.PaymentProviderId equals paymentProvider.Id
            where wallet.StakeholderId == request.StakeholderId
                && walletTransaction.Id == request.WalletTransactionId
                && paymentTransaction.PaymentIntent == PaymentIntent.WalletTopUp
            select new StakeholderWalletTopUpTransactionDetailReadModel(
                walletTransaction.Id,
                walletTransaction.TransactionTitle,
                walletTransaction.Description,
                walletTransaction.MerchantReference,
                walletTransaction.Amount,
                currency.CurrencyCode,
                paymentTransaction.PaymentMethodType,
                paymentProvider.ProviderName,
                walletTransaction.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }                                                                   
}
