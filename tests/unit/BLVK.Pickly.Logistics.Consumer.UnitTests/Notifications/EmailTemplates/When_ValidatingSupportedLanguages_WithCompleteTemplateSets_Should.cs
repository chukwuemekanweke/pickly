using BLVK.Pickly.Logistics.Domain.Common.Localization;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Notifications.EmailTemplates;

public sealed class When_ValidatingSupportedLanguages_WithCompleteTemplateSets_Should
{
    private static readonly string[] RequiredTemplates =
    [
        "AccountCreated.html",
        "AccountLocked.html",
        "CancelledSubscription.html",
        "ConfirmEmail.html",
        "EmailConfirmationFollowUp.html",
        "Invoice.html",
        "PasswordResetSuccessful.html",
        "ResetPassword.html",
        "SignInSuccessful.html",
        "TrialExpired.html"
    ];

    [Fact]
    public void ContainEveryRequiredEmailTemplate()
    {
        var templateSetsRoot = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "TemplateSets", "default");

        foreach (var language in SupportedLanguages.All)
        {
            File.Exists(Path.Combine(templateSetsRoot, language, "BaseTemplate.html")).ShouldBeTrue();
            var notificationDirectory = Path.Combine(templateSetsRoot, language, "NotificationTypes");
            Directory.GetFiles(notificationDirectory, "*.html")
                .Select(Path.GetFileName)
                .OrderBy(fileName => fileName)
                .ShouldBe(RequiredTemplates.OrderBy(fileName => fileName));
        }
    }
}
