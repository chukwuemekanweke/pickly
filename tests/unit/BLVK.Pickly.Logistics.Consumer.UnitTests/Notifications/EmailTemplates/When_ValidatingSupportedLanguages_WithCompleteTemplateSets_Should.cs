using BLVK.Pickly.Logistics.Domain.Common.Localization;
using Shouldly;
using System.Text.RegularExpressions;

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
        "OrderCreated.html",
        "PasswordResetSuccessful.html",
        "PaymentFailed.html",
        "PaymentInitiated.html",
        "PaymentSucceeded.html",
        "ResetPassword.html",
        "RiderAcceptedInstructions.html",
        "RiderAssigned.html",
        "SignInSuccessful.html",
        "TrialExpired.html"
    ];
    private static readonly string[] DeliveryTemplates =
    [
        "OrderCreated.html",
        "PaymentFailed.html",
        "PaymentInitiated.html",
        "PaymentSucceeded.html",
        "RiderAcceptedInstructions.html",
        "RiderAssigned.html"
    ];
    private static readonly Regex PlaceholderPattern = new(
        @"\{\{:(?<key>[A-Za-z0-9_]+):\}\}",
        RegexOptions.CultureInvariant);

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

    [Fact]
    public void RenderEveryDeliveryTemplateWithHostedIllustrationsAndNoUnresolvedTokens()
    {
        var templateSetsRoot = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "TemplateSets", "default");
        var replacements = new Dictionary<string, string>
        {
            ["ActionUrl"] = "https://app.pikly.ng/deliveries/PKL-TEST",
            ["Amount"] = "2,500.00",
            ["Currency"] = "NGN",
            ["CurrentStatus"] = "Pending assignment",
            ["DeliveryInstructions"] = "Call on arrival",
            ["DeliveryType"] = "Instant",
            ["DropOffAddress"] = "2 Dropoff, Lagos",
            ["DropOffInstructions"] = "Blue gate",
            ["FailureDescription"] = "The provider confirmed the attempt failed.",
            ["FirstName"] = "Ada",
            ["IllustrationUrl"] =
                "https://pub-0c671c99962543c1a440d1d9f1ce3a90.r2.dev/BLVK.Pickly.Logistics/email/transactional/v1/illustration.png",
            ["Items"] = "Parcel x1",
            ["NextSteps"] = "Open the delivery for the next step.",
            ["PaymentDate"] = "20 Oct 2026, 1:00 PM WAT",
            ["PaymentExpiry"] = "20 Oct 2026, 1:00 PM WAT",
            ["PaymentInstructions"] = "Bank: Test Bank",
            ["PaymentMethod"] = "BankTransfer",
            ["PaymentProvider"] = "SafeHaven",
            ["PaymentReference"] = "payment-reference",
            ["PaymentStatus"] = "Paid",
            ["PickupAddress"] = "1 Pickup, Lagos",
            ["PickupInstructions"] = "Warehouse gate",
            ["RecipientName"] = "Ada Recipient",
            ["RecipientPhone"] = "+2348012345678",
            ["RiderName"] = "Pikly Rider",
            ["ScheduledPickup"] = "As soon as possible",
            ["TrackingId"] = "PKL-TEST"
        };

        foreach (var language in SupportedLanguages.All)
        {
            var baseTemplate = File.ReadAllText(Path.Combine(templateSetsRoot, language, "BaseTemplate.html"));
            foreach (var templateFileName in DeliveryTemplates)
            {
                var template = File.ReadAllText(Path.Combine(
                    templateSetsRoot,
                    language,
                    "NotificationTypes",
                    templateFileName));
                var body = PlaceholderPattern.Replace(
                    template,
                    match => replacements[match.Groups["key"].Value]);
                var rendered = baseTemplate
                    .Replace("{{:Subject:}}", "Pikly delivery update", StringComparison.Ordinal)
                    .Replace("{{:BodyHtml:}}", body, StringComparison.Ordinal);

                rendered.ShouldNotContain("{{:");
                rendered.ShouldContain(replacements["IllustrationUrl"]);
                rendered.ShouldContain("alt=\"");
                rendered.ShouldContain("width=\"220\"");
                rendered.ShouldContain(replacements["TrackingId"]);
                rendered.ShouldContain("hello@pikly.org");
            }
        }
    }
}
