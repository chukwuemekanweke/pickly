using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using FluentValidation.Results;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests;

public sealed class WhenMappingValidationResult_Should
{
    [Fact]
    public void GroupMessagesByProperty()
    {
        const string emailProperty = "Email";
        const string passwordProperty = "Password";
        const string requiredEmailMessage = "Email is required.";
        const string invalidEmailMessage = "Email must be valid.";
        const string requiredPasswordMessage = "Password is required.";

        var validationResult = new ValidationResult([
            new ValidationFailure(emailProperty, requiredEmailMessage),
            new ValidationFailure(emailProperty, invalidEmailMessage),
            new ValidationFailure(passwordProperty, requiredPasswordMessage)
        ]);

        var dictionary = validationResult.ToValidationDictionary();

        dictionary[emailProperty].ShouldBe([requiredEmailMessage, invalidEmailMessage]);
        dictionary[passwordProperty].ShouldBe([requiredPasswordMessage]);
    }
}

