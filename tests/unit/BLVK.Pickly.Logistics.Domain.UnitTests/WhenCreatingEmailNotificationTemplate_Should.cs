using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests;

public sealed class WhenCreatingEmailNotificationTemplate_Should
{
    [Fact]
    public void SetFieldsAndAuditDates()
    {
        var utcNow = DateTimeOffset.UtcNow;

        var template = EmailNotificationTemplate.Create(NotificationType.SignInSuccessful, " Sign-in successful notification ", " Successful sign-in ", " SignInSuccessful.html ");

        template.NotificationType.ShouldBe(NotificationType.SignInSuccessful);
        template.Description.ShouldBe("Sign-in successful notification");
        template.Subject.ShouldBe("Successful sign-in");
        template.TemplateFileName.ShouldBe("SignInSuccessful.html");
        template.CreatedAtUtc.ShouldBe(default);
        template.UpdatedAtUtc.ShouldBe(default);
    }
}


