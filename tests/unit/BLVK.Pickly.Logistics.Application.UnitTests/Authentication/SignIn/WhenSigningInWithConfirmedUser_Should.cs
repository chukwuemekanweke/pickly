using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenSigningInWithConfirmedUser_Should
{
    [Fact]
    public async Task ReturnAccessToken()
    {
        var email = AuthenticationTestData.Email();
        var password = AuthenticationTestData.StrongPassword();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var ipAddress = AuthenticationTestData.IpAddress();
        var userAgent = AuthenticationTestData.UserAgent();
        const string token = "signed-jwt";
        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(email);
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(
            user.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            firstName,
            lastName);

        var context = new AuthenticationFlowTestContext();
        var expectedToken = new AccessToken(token, now.AddHours(1));
        var expectedRefreshToken = new RefreshToken("refresh-token", now.AddDays(30));

        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.IdentityService.CheckPasswordAsync(user, password).Returns(true);
        context.StakeholderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        context.AccessTokenService.Generate(user, stakeholder.Id, Arg.Any<Guid>()).Returns(expectedToken);
        context.RefreshTokenService.IssueAsync(user, Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(expectedRefreshToken);

        var result = await context.CreateSignInHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignInCommand(
                email,
                password,
                ipAddress,
                userAgent),
            CancellationToken.None);

        result.Status.ShouldBe(SignInStatus.Success);
        result.Tokens.ShouldNotBeNull();
        result.Tokens.AccessToken.ShouldBe(expectedToken);
        result.Tokens.RefreshToken.ShouldBe(expectedRefreshToken);
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserSignInSuccessful>(message =>
                message.IpAddress == ipAddress &&
                message.UserAgent == userAgent &&
                message.StakeholderId == stakeholder.Id),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}








