using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Payments.CreditWallet;

public sealed class When_HandlingCreditWallet_WithExistingWallet_Should
{
    [Fact]
    public async Task CreditWallet()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var currencyId = Guid.CreateVersion7();
        var command = context.CreateCreditWalletCommand(2500m, currencyId);
        var currency = context.CreateCurrency(currencyId, "NGN");
        var existingWallet = Wallet.Create(command.StakeholderId!.Value, command.TenantId, currencyId);
        Wallet? capturedWallet = null;
        WalletTransaction? capturedTransaction = null;

        context.WalletTransactionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<WalletTransaction>>(), Arg.Any<CancellationToken>())
            .Returns((WalletTransaction?)null);
        context.CurrencyRepository.GetByIdAsync(currencyId, Arg.Any<CancellationToken>())
            .Returns(currency);
        context.WalletRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Wallet>>(), Arg.Any<CancellationToken>())
            .Returns(existingWallet);
        context.WalletRepository.When(repo => repo.Update(Arg.Any<Wallet>())).Do(callInfo => capturedWallet = callInfo.Arg<Wallet>());
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
        await context.WalletRepository.DidNotReceive().AddAsync(Arg.Any<Wallet>(), Arg.Any<CancellationToken>());
    }
}

