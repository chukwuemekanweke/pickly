using Bogus;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

internal static class InfrastructureTestData
{
    private static readonly Faker Faker = new();

    public static string Email() => Faker.Internet.Email().ToLowerInvariant();

    public static string FirstName() => Faker.Name.FirstName();

    public static string LastName() => Faker.Name.LastName();
}
