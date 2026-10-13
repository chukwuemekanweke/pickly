using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Payments.CreditWallet;

public sealed class When_HandlingCreditWallet_WithNewWallet_Should
{
    [Fact]
    public async Task CreditWallet()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var currencyId = Guid.CreateVersion7();
        var command = context.CreateCreditWalletCommand(2500m, currencyId);
        var currency = context.CreateCurrency(currencyId, "NGN");
        Wallet? capturedWallet = null;
        WalletTransaction? capturedTransaction = null;

        context.WalletTransactionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<WalletTransaction>>(), Arg.Any<CancellationToken>())
            .Returns((WalletTransaction?)null);
        context.CurrencyRepository.GetByIdAsync(currencyId, Arg.Any<CancellationToken>())
            .Returns(currency);
        context.WalletRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Wallet>>(), Arg.Any<CancellationToken>())
            .Returns((Wallet?)null);
        context.WalletRepository.AddAsync(Arg.Do<Wallet>(wallet => capturedWallet = wallet), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        context.WalletTransactionRepository.AddAsync(Arg.Do<WalletTransaction>(transaction => capturedTransaction = transaction), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await context.CreateCreditWalletHandler().HandleAsync(command, CancellationToken.None);

        capturedWallet.ShouldNotBeNull();
        capturedWallet.Balance.ShouldBe(2500m);
        capturedTransaction.ShouldNotBeNull();
        capturedTransaction.PaymentTransactionId.ShouldBe(command.PaymentTransactionId);
        capturedTransaction.Amount.ShouldBe(2500m);
        capturedTransaction.TransactionType.ShouldBe(WalletTransactionType.Credit);
        capturedTransaction.TransactionCategory.ShouldBe(WalletTransactionCategory.WalletFunding);
        capturedTransaction.TransactionTitle.ShouldBe(WalletTransactionTitles.WalletFunding);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        context.WalletRepository.DidNotReceive().Update(Arg.Any<Wallet>());
    }
}
