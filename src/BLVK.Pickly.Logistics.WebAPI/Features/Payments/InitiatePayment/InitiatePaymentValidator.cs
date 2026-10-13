using BLVK.Pickly.Logistics.Contracts.Payments;
using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Payments.InitiatePayment;

public sealed class InitiatePaymentValidator : AbstractValidator<InitiatePaymentRequest>
{
    public InitiatePaymentValidator()
    {
        RuleFor(request => request.Amount)
            .GreaterThan(0);

        RuleFor(request => request.CurrencyId)
            .NotEmpty();

        RuleFor(request => request.PaymentProviderId)
            .NotEmpty();

        RuleFor(request => request.PaymentIntent)
            .NotEmpty()
            .Must(paymentIntent => Enum.TryParse<PaymentIntent>(paymentIntent, true, out _))
            .WithMessage("PaymentIntent must be one of: WalletTopUp, Subscription.");
    }
}
