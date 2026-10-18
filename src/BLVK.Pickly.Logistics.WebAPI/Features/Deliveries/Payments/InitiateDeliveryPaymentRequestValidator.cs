using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Payments;

public sealed class InitiateDeliveryPaymentRequestValidator : AbstractValidator<InitiateDeliveryPaymentRequest>
{
    public InitiateDeliveryPaymentRequestValidator() =>
        RuleFor(request => request.PaymentProviderId).NotEmpty();
}
