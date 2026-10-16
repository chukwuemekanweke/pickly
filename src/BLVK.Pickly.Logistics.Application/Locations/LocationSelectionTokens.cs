using System.Globalization;
using System.Security.Cryptography;

namespace BLVK.Pickly.Logistics.Application.Locations;

public static class LocationSelectionTokens
{
    private const int SelectionIdLength = 32;

    public static string NewSelectionId() => RandomNumberGenerator.GetHexString(SelectionIdLength, lowercase: true);

    public static string Create(string selectionId, int candidateIndex) =>
        $"{selectionId}.{candidateIndex.ToString(CultureInfo.InvariantCulture)}";

    public static bool TryParse(string? token, out string selectionId, out int candidateIndex)
    {
        selectionId = string.Empty;
        candidateIndex = -1;
        var parts = token?.Split('.') ?? [];
        if (parts.Length != 2 ||
            parts[0].Length != SelectionIdLength ||
            !parts[0].All(character => char.IsAsciiDigit(character) || character is >= 'a' and <= 'f') ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out candidateIndex))
        {
            return false;
        }

        selectionId = parts[0];
        return true;
    }

    public static string SelectionCacheKey(string selectionId) => $"locations:selection:{selectionId}";

    public static string OutcomeCacheKey(string resolutionKey) => $"locations:geocoding-outcome:{resolutionKey}";
}
