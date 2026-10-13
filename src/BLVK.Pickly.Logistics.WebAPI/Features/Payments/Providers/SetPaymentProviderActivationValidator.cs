using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Payments.Providers;

public sealed class SetPaymentProviderActivationValidator : AbstractValidator<SetPaymentProviderActivationRequest>
{
    public SetPaymentProviderActivationValidator()
    {
    }
}
