using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Registrations;

public sealed class When_ValidatingGoogleRegistration_WithMissingProfileDetails_Should
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ApplyActiveRegistrationPolicy(bool isMvp, bool expectedValid)
    {
        var policyProvider = Substitute.For<IRegistrationPolicyProvider>();
        policyProvider.GetAsync().Returns(RegistrationPolicy.Create(isMvp));
        var validator = new GoogleSignUpValidator(policyProvider);
        var request = new GoogleSignUpRequest("google-flow-token");

        var result = await validator.ValidateAsync(request);

        result.IsValid.ShouldBe(expectedValid);
    }
}
