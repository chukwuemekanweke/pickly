using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Payments.WalletTransactions;

public sealed class GetStakeholderWalletTransactionsValidator : AbstractValidator<GetStakeholderWalletTransactionsRequest>
{
    public GetStakeholderWalletTransactionsValidator()
    {
        RuleFor(request => request.Limit)
            .InclusiveBetween(1, 100);
    }
}
