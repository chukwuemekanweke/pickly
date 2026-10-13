using System.Runtime.CompilerServices;

namespace BLVK.Pickly.Logistics.Observability.IntegrationTests.Infrastructure;

internal static class RepositoryPaths
{
    private static readonly string RepositoryRoot = ResolveRepositoryRoot();

    public static string OtelCollectorConfigPath { get; } =
        Path.Combine(RepositoryRoot, "observability", "otel-collector", "otel-collector.yml");

    public static string TempoConfigPath { get; } =
        Path.Combine(RepositoryRoot, "observability", "tempo", "tempo.yml");

    private static string ResolveRepositoryRoot([CallerFilePath] string sourceFilePath = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "..", "..", "..", ".."));
}
