using System.Security.Cryptography;
using System.Text;

namespace BLVK.Pickly.Logistics.Contracts.Commands.Notifications;

public static class NotificationMessageIdentity
{
    private static readonly Guid NamespaceId = new("574244df-4bb2-5fc3-978d-b105cce4d874");

    public static Guid Create(Guid transitionId, NotificationType notificationType, Guid recipientId)
    {
        var name = Encoding.UTF8.GetBytes(
            $"{NamespaceId:N}:{transitionId:N}:{(int)notificationType}:{recipientId:N}");
        var hash = SHA256.HashData(name);
        Span<byte> legacyBytes = stackalloc byte[16];
        hash.AsSpan(0, legacyBytes.Length).CopyTo(legacyBytes);
        legacyBytes[6] = (byte)((legacyBytes[6] & 0x0f) | 0x50);
        legacyBytes[8] = (byte)((legacyBytes[8] & 0x3f) | 0x80);

        // Preserve the deterministic bits emitted by the former UUIDv5 implementation. Only the version nibble
        // changes, which satisfies the transport's UUIDv7 contract without weakening replay deduplication.
        Span<char> identity = stackalloc char[32];
        new Guid(legacyBytes).TryFormat(identity, out _, "N");
        identity[12] = '7';
        return Guid.ParseExact(identity, "N");
    }
}
