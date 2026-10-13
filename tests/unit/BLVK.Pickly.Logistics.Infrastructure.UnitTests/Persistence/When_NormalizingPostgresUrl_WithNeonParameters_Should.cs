using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Npgsql;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Persistence;

public sealed class When_NormalizingPostgresUrl_WithNeonParameters_Should
{
    [Fact]
    public void ReturnNpgsqlConnectionString()
    {
        const string databaseUrl = "postgresql://neon%40user:p%40ssword@ep-example.neon.tech/neondb?sslmode=require&channel_binding=require";

        var result = PostgresConnectionString.Normalize(databaseUrl);

        var connectionString = new NpgsqlConnectionStringBuilder(result);
        connectionString.Host.ShouldBe("ep-example.neon.tech");
        connectionString.Port.ShouldBe(5432);
        connectionString.Database.ShouldBe("neondb");
        connectionString.Username.ShouldBe("neon@user");
        connectionString.Password.ShouldBe("p@ssword");
        connectionString.SslMode.ShouldBe(SslMode.Require);
        connectionString.ChannelBinding.ShouldBe(ChannelBinding.Require);
    }
}
