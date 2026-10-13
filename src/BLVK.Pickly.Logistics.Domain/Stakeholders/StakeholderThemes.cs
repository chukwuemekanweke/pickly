namespace BLVK.Pickly.Logistics.Domain.Stakeholders;

public static class StakeholderThemes
{
    public const string Default = System;
    public const string System = "system";
    public const string Light = "light";
    public const string Dark = "dark";

    public static readonly IReadOnlyCollection<string> All = [System, Light, Dark];

    public static string Normalize(string theme)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(theme);
        var normalized = theme.Trim().ToLowerInvariant();

        if (!All.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Theme '{theme}' is not supported.", nameof(theme));
        }

        return normalized;
    }
}