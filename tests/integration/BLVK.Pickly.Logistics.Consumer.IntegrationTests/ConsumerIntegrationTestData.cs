using Bogus;

namespace BLVK.Pickly.Logistics.Consumer.IntegrationTests;

internal static class ConsumerIntegrationTestData
{
    private static readonly Faker Faker = new();

    public static string Email() => Faker.Internet.Email().ToLowerInvariant();

    public static string FirstName() => Faker.Name.FirstName();

    public static string LastName() => Faker.Name.LastName();

    public static string IpAddress() => Faker.Internet.Ip();

    public static string UserAgent() => Faker.Internet.UserAgent();
}
