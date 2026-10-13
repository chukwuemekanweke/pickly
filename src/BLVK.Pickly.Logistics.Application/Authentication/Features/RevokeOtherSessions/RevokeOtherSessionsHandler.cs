using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RevokeOtherSessions;

public sealed class RevokeOtherSessionsHandler(IAuthenticationSessionService sessionService, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(RevokeOtherSessionsCommand command, CancellationToken cancellationToken)
    {
        await sessionService.RevokeOthersAsync(command.CurrentSessionId, command.StakeholderId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
