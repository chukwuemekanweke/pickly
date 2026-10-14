using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Registrations;

public sealed class When_ValidatingRegistration_WithMissingProfileDetails_Should
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ApplyActiveRegistrationPolicy(bool isMvp, bool expectedValid)
    {
        var policyProvider = Substitute.For<IRegistrationPolicyProvider>();
        policyProvider.GetAsync().Returns(RegistrationPolicy.Create(isMvp));
        var validator = new SignUpValidator(policyProvider);
        var request = new SignUpRequest("jane@example.com", "P@ssw0rd123!", "P@ssw0rd123!");

        var result = await validator.ValidateAsync(request);

        result.IsValid.ShouldBe(expectedValid);
    }
}
