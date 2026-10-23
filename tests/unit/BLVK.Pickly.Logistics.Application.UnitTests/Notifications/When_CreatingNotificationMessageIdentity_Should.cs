using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using Shouldly;
using System.Security.Cryptography;
using System.Text;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Notifications;

public sealed class When_CreatingNotificationMessageIdentity_Should
{
    [Fact]
    public void Return_A_Deterministic_UuidV7()
    {
        var transitionId = Guid.CreateVersion7();
        var recipientId = Guid.CreateVersion7();

        var first = NotificationMessageIdentity.Create(
            transitionId,
            NotificationType.OrderCreated,
            recipientId);
        var replay = NotificationMessageIdentity.Create(
            transitionId,
            NotificationType.OrderCreated,
            recipientId);

        replay.ShouldBe(first);
        first.Version.ShouldBe(7);
    }

    [Fact]
    public void Return_Different_Identities_For_Different_Notifications()
    {
        var transitionId = Guid.CreateVersion7();
        var recipientId = Guid.CreateVersion7();

        var orderCreated = NotificationMessageIdentity.Create(
            transitionId,
            NotificationType.OrderCreated,
            recipientId);
        var riderAssigned = NotificationMessageIdentity.Create(
            transitionId,
            NotificationType.RiderAssigned,
            recipientId);

        riderAssigned.ShouldNotBe(orderCreated);
    }

    [Fact]
    public void Return_A_UuidV7_For_A_Legacy_Transition_Identifier()
    {
        var identity = NotificationMessageIdentity.Create(
            Guid.Parse("8a350d29-30d4-4bbc-ab79-614b53c974b2"),
            NotificationType.PaymentSucceeded,
            Guid.CreateVersion7());

        identity.Version.ShouldBe(7);
    }

    [Fact]
    public void Preserve_The_Former_Deterministic_Identity_Bits()
    {
        var transitionId = Guid.Parse("019a19e7-4e35-7a11-bdf1-1ba00ff50abd");
        var recipientId = Guid.Parse("019a19e7-54ce-7b4e-9945-a45f29cf37dc");
        var notificationType = NotificationType.OrderCreated;
        var namespaceId = Guid.Parse("574244df-4bb2-5fc3-978d-b105cce4d874");
        var name = Encoding.UTF8.GetBytes(
            $"{namespaceId:N}:{transitionId:N}:{(int)notificationType}:{recipientId:N}");
        var hash = SHA256.HashData(name);
        Span<byte> legacyBytes = stackalloc byte[16];
        hash.AsSpan(0, legacyBytes.Length).CopyTo(legacyBytes);
        legacyBytes[6] = (byte)((legacyBytes[6] & 0x0f) | 0x50);
        legacyBytes[8] = (byte)((legacyBytes[8] & 0x3f) | 0x80);
        var legacyText = new Guid(legacyBytes).ToString("N");
        var upgradedLegacy = Guid.ParseExact($"{legacyText[..12]}7{legacyText[13..]}", "N");

        var identity = NotificationMessageIdentity.Create(transitionId, notificationType, recipientId);

        identity.ShouldBe(upgradedLegacy);
    }
}
