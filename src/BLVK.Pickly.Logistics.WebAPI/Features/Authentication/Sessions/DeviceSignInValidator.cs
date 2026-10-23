using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;

public sealed class DeviceSignInValidator : AbstractValidator<DeviceSignInRequest>
{
    public DeviceSignInValidator()
    {
        RuleFor(request => request.DeviceIdentifier)
            .NotEmpty()
            .MaximumLength(DeviceAccount.MaxIdentifierLength);
    }
}
