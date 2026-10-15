using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Locations;

public sealed class SelectLocationCandidateRequestValidator : AbstractValidator<SelectLocationCandidateRequest>
{
    public SelectLocationCandidateRequestValidator()
    {
        RuleFor(request => request.SelectionToken).NotEmpty().MaximumLength(64);
    }
}
