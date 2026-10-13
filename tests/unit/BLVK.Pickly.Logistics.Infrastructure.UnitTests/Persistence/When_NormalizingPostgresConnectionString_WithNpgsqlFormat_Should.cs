using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Persistence;

public sealed class When_NormalizingPostgresConnectionString_WithNpgsqlFormat_Should
{
    [Fact]
    public void PreserveConnectionString()
    {
        const string connectionString = "Host=localhost;Database=BLVK.Pickly.LogisticsDb;Username=postgres;Password=postgres;";

        var result = PostgresConnectionString.Normalize(connectionString);

        result.ShouldBe(connectionString);
    }
}
