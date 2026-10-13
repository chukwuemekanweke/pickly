using Npgsql;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public static class PostgresConnectionString
{
    public static string Normalize(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2)
        {
            throw new FormatException("PostgreSQL URLs must contain both a username and password.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1])
        };

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = parameter.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

            builder[NormalizeParameterName(key)] = value;
        }

        return builder.ConnectionString;
    }

    private static string NormalizeParameterName(string name) =>
        name.ToLowerInvariant() switch
        {
            "channel_binding" => "Channel Binding",
            "connect_timeout" => "Timeout",
            "sslmode" => "SSL Mode",
            _ => name.Replace('_', ' ')
        };
}
