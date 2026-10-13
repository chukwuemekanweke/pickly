namespace BLVK.Pickly.Logistics.Domain.Common.Localization;

public static class SupportedLanguages
{
    public const string Default = English;
    public const string English = "en";
    public const string French = "fr";

    public static readonly IReadOnlyCollection<string> All = [English, French];

    public static bool IsSupported(string? language) =>
        language is not null && All.Contains(language, StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        var normalized = language.Trim().ToLowerInvariant();

        if (!IsSupported(normalized))
        {
            throw new ArgumentException($"Language '{language}' is not supported.", nameof(language));
        }

        return normalized;
    }

}
