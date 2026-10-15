using System.Globalization;
using System.Text.RegularExpressions;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;

public static partial class LocationSearchTerms
{
    public const int MaxQueryLength = 100;
    public const int MaxTerms = 8;
    public const int MaxTermLength = 40;

    /// <summary>
    /// Reduces free text to bounded letter/digit terms so the registry search can use an indexed full-text prefix
    /// match instead of a leading-wildcard scan, and so no query syntax from the client reaches the database.
    /// </summary>
    public static IReadOnlyList<string> Extract(string? query) =>
        TermRegex()
            .Matches(query ?? string.Empty)
            .Select(match => match.Value.ToLower(CultureInfo.InvariantCulture))
            .Select(term => term.Length > MaxTermLength ? term[..MaxTermLength] : term)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxTerms)
            .ToArray();

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex TermRegex();
}
