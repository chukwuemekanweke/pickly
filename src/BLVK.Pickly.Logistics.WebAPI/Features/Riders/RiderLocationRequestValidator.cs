using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Riders;

public sealed class RiderLocationRequestValidator : AbstractValidator<RiderLocationRequest>
{
    public RiderLocationRequestValidator()
    {
        RuleFor(request => request.Latitude).InclusiveBetween(-90m, 90m);
        RuleFor(request => request.Longitude).InclusiveBetween(-180m, 180m);
        RuleFor(request => request.CapturedAtUtc).NotEmpty();
    }
}
