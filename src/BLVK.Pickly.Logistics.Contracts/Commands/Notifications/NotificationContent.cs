using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.Contracts.Commands.Notifications;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "contentType")]
[JsonDerivedType(typeof(EmailNotificationContent), "email")]
[JsonDerivedType(typeof(SmsNotificationContent), "sms")]
[JsonDerivedType(typeof(WebPushNotificationContent), "webPush")]
[JsonDerivedType(typeof(AppPushNotificationContent), "appPush")]
public abstract record NotificationContent(Dictionary<string, string> Content);
