namespace BLVK.Pickly.Logistics.WebAPI.Infrastructure;

public static class ClientIpAddressExtensions
{
    public static string GetClientIpAddress(this HttpContext? httpContext)
    {
        var address = httpContext?.Connection.RemoteIpAddress;
        if (address?.IsIPv4MappedToIPv6 == true)
        {
            address = address.MapToIPv4();
        }

        return address?.ToString() ?? string.Empty;
    }
}
