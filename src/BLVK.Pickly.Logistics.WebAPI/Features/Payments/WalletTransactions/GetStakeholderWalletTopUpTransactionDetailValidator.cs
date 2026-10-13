using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Payments.WalletTransactions;

public sealed class GetStakeholderWalletTopUpTransactionDetailValidator : AbstractValidator<GetStakeholderWalletTopUpTransactionDetailRequest>
{
    public GetStakeholderWalletTopUpTransactionDetailValidator()
    {
        RuleFor(request => request.WalletTransactionId)
            .NotEmpty();
    }
}
