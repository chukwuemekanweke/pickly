using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using System.Security.Cryptography;

namespace BLVK.Pickly.Logistics.Infrastructure.Deliveries;

internal sealed class DeliveryTrackingIdGenerator : IDeliveryTrackingIdGenerator
{
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const int RandomLength = 8;

    public string Generate()
    {
        Span<char> value = stackalloc char[RandomLength + 4];
        "PKL-".AsSpan().CopyTo(value);

        for (var index = 4; index < value.Length; index++)
        {
            value[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(value);
    }
}
