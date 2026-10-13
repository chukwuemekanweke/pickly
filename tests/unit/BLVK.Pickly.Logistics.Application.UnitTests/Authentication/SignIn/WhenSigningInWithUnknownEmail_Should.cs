using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenSigningInWithUnknownEmail_Should
{
    [Fact]
    public async Task ReturnInvalidCredentialsAndPublishFailedEvent()
    {
        var email = AuthenticationTestData.Email();
        var password = AuthenticationTestData.StrongPassword();
        var ipAddress = AuthenticationTestData.IpAddress();
        var userAgent = AuthenticationTestData.UserAgent();

        var context = new AuthenticationFlowTestContext();
        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);

        var result = await context.CreateSignInHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignInCommand(
                email,
                password,
                ipAddress,
                userAgent),
            CancellationToken.None);

        result.Status.ShouldBe(SignInStatus.InvalidCredentials);
        result.Tokens.ShouldBeNull();
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserSignInFailed>(message =>
                message.EmailAddress == email &&
                message.IpAddress == ipAddress &&
                message.UserAgent == userAgent &&
                message.FailureReason == "user_not_found"),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

