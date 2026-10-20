using BLVK.Pickly.Logistics.Domain.Common.Localization;
using Shouldly;
using System.Net;
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
    private static readonly Dictionary<string, string[]> LegacyTemplatePlaceholders = new(StringComparer.Ordinal)
    {
        ["AccountCreated.html"] = ["DashboardUrl", "FirstName", "IllustrationUrl", "Product"],
        ["AccountLocked.html"] = ["IllustrationUrl", "LockedUntilUtc"],
        ["CancelledSubscription.html"] = ["FeedbackUrl", "IllustrationUrl", "Product"],
        ["ConfirmEmail.html"] = ["IllustrationUrl", "OtpCode", "OtpExpiresAtUtc", "Product", "VerifyUrl"],
        ["EmailConfirmationFollowUp.html"] = ["IllustrationUrl", "OtpCode", "Product", "VerifyUrl"],
        ["Invoice.html"] = ["AmountDue", "Currency", "DownloadInvoiceUrl", "FirstName", "IllustrationUrl", "InvoiceDate", "InvoiceNumber"],
        ["PasswordResetSuccessful.html"] = ["IllustrationUrl"],
        ["ResetPassword.html"] = ["FirstName", "IllustrationUrl", "LastName", "OtpCode", "OtpExpiresAtUtc"],
        ["SignInSuccessful.html"] = ["IllustrationUrl", "IpAddress", "UserAgent"],
        ["TrialExpired.html"] = ["BillingPortalUrl", "IllustrationUrl", "Product"]
    };
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
    public void RenderEveryTemplateWithEncodedReplacementsAndNoUnresolvedTokens()
    {
        var templateSetsRoot = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "TemplateSets", "default");
        var replacements = new Dictionary<string, string>
        {
            ["ActionUrl"] = "https://app.pikly.org/deliveries/PKL-TEST",
            ["Amount"] = "2,500.00",
            ["AmountDue"] = "2,500.00",
            ["BillingPortalUrl"] = "https://app.pikly.org/billing?source=email&plan=standard",
            ["Currency"] = "NGN",
            ["CurrentStatus"] = "Pending assignment",
            ["DashboardUrl"] = "https://app.pikly.org/dashboard?source=email&next=deliveries",
            ["DeliveryInstructions"] = "Call on arrival",
            ["DeliveryType"] = "Instant",
            ["DownloadInvoiceUrl"] = "https://app.pikly.org/invoices/INV-1001?download=true&source=email",
            ["DropOffAddress"] = "2 Dropoff, Lagos",
            ["DropOffInstructions"] = "Blue gate",
            ["FailureDescription"] = "The provider confirmed the attempt failed.",
            ["FeedbackUrl"] = "https://pikly.org/feedback?source=email&topic=subscription",
            ["FirstName"] = "Ada <script>alert('unsafe')</script>",
            ["IllustrationUrl"] =
                "https://pub-0c671c99962543c1a440d1d9f1ce3a90.r2.dev/BLVK.Pickly.Logistics/email/transactional/v1/illustration.png",
            ["InvoiceDate"] = "20 Oct 2026",
            ["InvoiceNumber"] = "INV-1001",
            ["IpAddress"] = "203.0.113.10",
            ["Items"] = "Parcel x1",
            ["LastName"] = "Nwosu",
            ["LockedUntilUtc"] = "20 Oct 2026, 1:00 PM WAT",
            ["NextSteps"] = "Open the delivery for the next step.",
            ["OtpCode"] = "123456",
            ["OtpExpiresAtUtc"] = "20 Oct 2026, 1:00 PM WAT",
            ["PaymentDate"] = "20 Oct 2026, 1:00 PM WAT",
            ["PaymentExpiry"] = "20 Oct 2026, 1:00 PM WAT",
            ["PaymentInstructions"] = "Bank: Test Bank",
            ["PaymentMethod"] = "BankTransfer",
            ["PaymentProvider"] = "SafeHaven",
            ["PaymentReference"] = "payment-reference",
            ["PaymentStatus"] = "Paid",
            ["PickupAddress"] = "1 Pickup, Lagos",
            ["PickupInstructions"] = "Warehouse gate",
            ["Product"] = "Pikly",
            ["RecipientName"] = "Ada Recipient",
            ["RecipientPhone"] = "+2348012345678",
            ["RiderName"] = "Pikly Rider",
            ["ScheduledPickup"] = "As soon as possible",
            ["TrackingId"] = "PKL-TEST",
            ["UserAgent"] = "Browser <script>alert('unsafe')</script>",
            ["VerifyUrl"] = "https://app.pikly.org/verify?token=test&source=email"
        };

        foreach (var language in SupportedLanguages.All)
        {
            var baseTemplate = File.ReadAllText(Path.Combine(templateSetsRoot, language, "BaseTemplate.html"));
            foreach (var templateFileName in RequiredTemplates)
            {
                var template = File.ReadAllText(Path.Combine(
                    templateSetsRoot,
                    language,
                    "NotificationTypes",
                    templateFileName));
                var body = PlaceholderPattern.Replace(
                    template,
                    match => WebUtility.HtmlEncode(replacements[match.Groups["key"].Value]));
                var rendered = baseTemplate
                    .Replace("{{:Subject:}}", "Pikly delivery update", StringComparison.Ordinal)
                    .Replace("{{:BodyHtml:}}", body, StringComparison.Ordinal);

                rendered.ShouldNotContain("{{:");
                rendered.ShouldContain("hello@pikly.org");
                rendered.ShouldNotContain("<script>");
                rendered.ShouldContain(replacements["IllustrationUrl"]);
                rendered.ShouldContain("alt=\"");
                rendered.ShouldContain("width=\"220\"");

                if (DeliveryTemplates.Contains(templateFileName, StringComparer.Ordinal))
                {
                    rendered.ShouldContain(replacements["TrackingId"]);
                }
            }
        }
    }

    [Fact]
    public void PreserveLegacyPlaceholderContractsAndBrandedStructure()
    {
        var templateSetsRoot = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "TemplateSets", "default");

        foreach (var (templateFileName, expectedPlaceholders) in LegacyTemplatePlaceholders)
        {
            foreach (var language in SupportedLanguages.All)
            {
                var template = File.ReadAllText(Path.Combine(
                    templateSetsRoot,
                    language,
                    "NotificationTypes",
                    templateFileName));
                var placeholders = PlaceholderPattern.Matches(template)
                    .Select(match => match.Groups["key"].Value)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(key => key, StringComparer.Ordinal);

                placeholders.ShouldBe(expectedPlaceholders.OrderBy(key => key, StringComparer.Ordinal));
                template.ShouldContain("<h1 style=\"");
                template.ShouldContain("#0a2e26");
                template.ShouldContain("#526a63");
            }
        }
    }

    [Fact]
    public void KeepSeededSubjectsCompatibleWithTemplatePayloads()
    {
        var seeder = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "EmailTemplates",
            "0001.template-seeder.postdeploy.sql"));
        var rowPattern = new Regex(
            @"\((?<type>\d+), '(?<language>en|fr)', '[^']*', '(?<subject>[^']*)', '(?<file>[^']+\.html)'\)",
            RegexOptions.CultureInvariant);
        var legacyRows = rowPattern.Matches(seeder)
            .Where(match => int.Parse(match.Groups["type"].Value) <= 10)
            .ToArray();

        legacyRows.Length.ShouldBe(LegacyTemplatePlaceholders.Count * SupportedLanguages.All.Count);
        foreach (var row in legacyRows)
        {
            var templateFileName = row.Groups["file"].Value;
            var subjectPlaceholders = PlaceholderPattern.Matches(row.Groups["subject"].Value)
                .Select(match => match.Groups["key"].Value);

            subjectPlaceholders.ShouldAllBe(placeholder =>
                LegacyTemplatePlaceholders[templateFileName].Contains(placeholder, StringComparer.Ordinal));
        }
    }
}
