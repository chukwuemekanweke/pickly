using BLVK.Pickly.Logistics.Domain.Locations;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Locations;

public sealed class GetNearbyLocationsRequestValidator : AbstractValidator<GetNearbyLocationsRequest>
{
    public GetNearbyLocationsRequestValidator(IOptions<LocationResolutionOptions> options)
    {
        var settings = options.Value;
        RuleFor(request => request.LocationId).NotEmpty();
        RuleFor(request => request.RadiusMeters).InclusiveBetween(50, settings.MaxNearbyRadiusMeters);
        RuleFor(request => request.Limit).InclusiveBetween(1, settings.MaxResults);
        RuleFor(request => request.LocationType).IsInEnum().When(request => request.LocationType.HasValue);
    }
}
