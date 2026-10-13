using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Registrations;

public sealed class When_HandlingRegistration_WithMismatchedIpCountry_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<SignUpRequest>>();
        var googleValidator = Substitute.For<IValidator<GoogleSignUpRequest>>();
        var request = new SignUpRequest(
            "jane@example.com",
            "P@ssw0rd123!",
            "P@ssw0rd123!",
            Guid.CreateVersion7(),
            "Jane",
            "Doe");
        validator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.IpGeolocationService.GetGeolocationAsync("8.8.8.8", Arg.Any<CancellationToken>())
            .Returns(new IpGeolocation("Paris", "Ile-de-France", "France"));
        var sut = new RegistrationsController(
            context.CreateSignUpHandler(),
            context.CreateGoogleSignUpHandler(),
            validator,
            googleValidator,
            context.CurrentActor)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Connection = { RemoteIpAddress = IPAddress.Parse("8.8.8.8") }
                }
            }
        };

        var result = await sut.Handle(request, CancellationToken.None);

        var problem = result.Result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }
}
