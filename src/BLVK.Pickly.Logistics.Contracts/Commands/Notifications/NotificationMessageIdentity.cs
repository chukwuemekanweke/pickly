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
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, bytes.Length).CopyTo(bytes);
        bytes[6] = (byte)((bytes[6] & 0x0f) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);
        return new Guid(bytes);
    }
}
