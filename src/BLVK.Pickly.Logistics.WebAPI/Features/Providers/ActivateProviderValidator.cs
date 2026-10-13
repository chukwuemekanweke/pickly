using BLVK.Pickly.Logistics.Domain.Providers.Entities;
using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Providers;

public sealed class ActivateProviderValidator : AbstractValidator<ActivateProviderRequest>
{
    public ActivateProviderValidator()
    {
        RuleFor(request => request.ProviderKey)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.ProviderType)
            .NotEmpty()
            .Must(providerType => Enum.TryParse<ProviderType>(providerType, true, out _))
            .WithMessage("ProviderType must be one of: Email, FileStorage.");
    }
}
