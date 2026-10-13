using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Passwords;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Passwords;

public sealed class When_ChangingPassword_WithMismatchedConfirmation_Should
{
    [Fact]
    public async Task RejectRequest()
    {
        var validator = new ChangePasswordValidator();
        var request = new ChangePasswordRequest("P@ssw0rd123!", "N3wP@ssword!", "DifferentP@ssword1!");

        var result = await validator.ValidateAsync(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(ChangePasswordRequest.ConfirmNewPassword));
    }
}
