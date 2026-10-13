using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.CheckEmailExistence;

public sealed class CheckEmailExistenceHandler(IAuthenticationIdentityService identityService)
{
    public async Task<CheckEmailExistenceResult> HandleAsync(
        CheckEmailExistenceCommand command,
        CancellationToken cancellationToken)
    {
        var user = await identityService.FindByEmailAsync(command.Email.Trim());
        return new CheckEmailExistenceResult(user is not null);
    }
}
