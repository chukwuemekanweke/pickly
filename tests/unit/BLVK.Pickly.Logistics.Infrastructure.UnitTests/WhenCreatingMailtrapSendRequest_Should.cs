using BLVK.Pickly.Logistics.Infrastructure.Notifications;
using Mailtrap.Emails.Requests;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class WhenCreatingMailtrapSendRequest_Should
{
    [Fact]
    public void MapDeliveryMessage()
    {
        var message = new EmailDeliveryMessage(
            "no-reply@test.local",
            "Backend Project Template",
            "to@test.local",
            "Subject line",
            "<html><body><p>Line 1</p><br /><p>Line 3</p></body></html>",
            ["cc1@test.local", "cc2@test.local"],
            ["bcc1@test.local"]);

        SendEmailRequest request = MailtrapEmailTransportProvider.CreateRequest(message);

        request.From.ShouldNotBeNull();
        request.From.Email.ShouldBe("no-reply@test.local");
        request.From.DisplayName.ShouldBe("Backend Project Template");
        request.To.Select(address => address.Email).ShouldBe(["to@test.local"]);
        request.Cc.Select(address => address.Email).ShouldBe(["cc1@test.local", "cc2@test.local"]);
        request.Bcc.Select(address => address.Email).ShouldBe(["bcc1@test.local"]);
        request.Subject.ShouldBe("Subject line");
        request.TextBody.ShouldBeNull();
        request.HtmlBody.ShouldBe("<html><body><p>Line 1</p><br /><p>Line 3</p></body></html>");
    }
}

