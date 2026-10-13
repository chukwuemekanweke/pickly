using BLVK.Pickly.Logistics.Application.Authentication.Features.GetLoginActivityHistory;
using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.LoginActivity;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Stakeholders.LoginActivity;

internal sealed class LoginActivityControllerTestContext
{
    public ILoginActivityReadModelRepository Repository { get; } = Substitute.For<ILoginActivityReadModelRepository>();
    public ICurrentActor CurrentActor { get; } = Substitute.For<ICurrentActor>();

    public LoginActivityController CreateController() =>
        new(new GetLoginActivityHistoryHandler(Repository), new GetLoginActivityValidator(), CurrentActor);
}
