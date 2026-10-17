using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Bookings;

public sealed class CreateDeliveryRequestValidator : AbstractValidator<CreateDeliveryRequest>
{
    public CreateDeliveryRequestValidator()
    {
        RuleFor(request => request.QuoteId).NotEmpty();
        RuleFor(request => request.Recipient).NotNull();
        When(request => request.Recipient is not null, () =>
        {
            RuleFor(request => request.Recipient.Name).NotEmpty().MaximumLength(200);
            RuleFor(request => request.Recipient.PhoneNumber)
                .NotEmpty()
                .Matches("^\\+[1-9][0-9]{7,14}$")
                .WithMessage("Recipient phone number must use international format.");
        });
        RuleFor(request => request.DropOffInstructions).MaximumLength(500);
    }
}
