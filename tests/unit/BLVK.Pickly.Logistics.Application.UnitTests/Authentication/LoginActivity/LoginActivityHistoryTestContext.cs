using BLVK.Pickly.Logistics.Application.Authentication.Features.GetLoginActivityHistory;
using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.LoginActivity;

internal sealed class LoginActivityHistoryTestContext
{
    public ILoginActivityReadModelRepository Repository { get; } = Substitute.For<ILoginActivityReadModelRepository>();

    public GetLoginActivityHistoryHandler CreateHandler() => new(Repository);
}
