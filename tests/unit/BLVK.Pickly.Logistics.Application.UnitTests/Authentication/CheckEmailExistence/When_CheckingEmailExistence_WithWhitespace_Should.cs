using BLVK.Pickly.Logistics.Application.Authentication.Features.CheckEmailExistence;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.CheckEmailExistence;

public sealed class When_CheckingEmailExistence_WithWhitespace_Should
{
    [Fact]
    public async Task TrimEmailBeforeLookup()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        context.IdentityService.FindByEmailAsync(email).Returns(context.CreateUser(email));

        var result = await context.CreateCheckEmailExistenceHandler().HandleAsync(
            new CheckEmailExistenceCommand($"  {email}  "),
            CancellationToken.None);

        result.Exists.ShouldBeTrue();
        await context.IdentityService.Received(1).FindByEmailAsync(email);
    }
}
