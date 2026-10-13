using Bogus;

namespace BLVK.Pickly.Logistics.Domain.UnitTests;

internal static class DomainTestData
{
    private static readonly Faker Faker = new();

    public static string Email() => Faker.Internet.Email().ToLowerInvariant();

    public static string FirstName() => Faker.Name.FirstName();

    public static string LastName() => Faker.Name.LastName();
}
