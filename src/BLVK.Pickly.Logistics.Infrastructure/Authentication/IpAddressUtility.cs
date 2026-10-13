using System.Net;
using System.Net.Sockets;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

internal static class IpAddressUtility
{
    public static bool IsPublicIpAddress(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out var parsedIpAddress))
        {
            return false;
        }

        if (IPAddress.IsLoopback(parsedIpAddress))
        {
            return false;
        }

        if (parsedIpAddress.IsIPv4MappedToIPv6)
        {
            parsedIpAddress = parsedIpAddress.MapToIPv4();
        }

        var bytes = parsedIpAddress.GetAddressBytes();

        return parsedIpAddress.AddressFamily switch
        {
            AddressFamily.InterNetwork => !IsPrivateIpv4(bytes),
            AddressFamily.InterNetworkV6 => !IsPrivateIpv6(bytes),
            _ => false
        };
    }

    private static bool IsPrivateIpv4(byte[] bytes) =>
        bytes[0] == 10 ||
        (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
        (bytes[0] == 192 && bytes[1] == 168) ||
        (bytes[0] == 169 && bytes[1] == 254) ||
        bytes[0] == 127;

    private static bool IsPrivateIpv6(byte[] bytes) =>
        bytes[0] == 0xfc ||
        bytes[0] == 0xfd ||
        (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80);
}
