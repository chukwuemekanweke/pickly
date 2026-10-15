using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using BLVK.Pickly.Logistics.Domain.Locations;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Locations;

public sealed class SearchLocationsRequestValidator : AbstractValidator<SearchLocationsRequest>
{
    public SearchLocationsRequestValidator(IOptions<LocationResolutionOptions> options)
    {
        RuleFor(request => request.Q)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(LocationSearchTerms.MaxQueryLength);
        RuleFor(request => request.CountryCode)
            .Matches("^[A-Za-z]{2}$")
            .When(request => !string.IsNullOrWhiteSpace(request.CountryCode))
            .WithMessage("CountryCode must be a two-letter ISO code.");
        RuleFor(request => request.Limit).InclusiveBetween(1, options.Value.MaxResults);
    }
}
