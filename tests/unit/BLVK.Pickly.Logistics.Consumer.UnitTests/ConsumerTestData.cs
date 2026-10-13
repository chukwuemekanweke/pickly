using Bogus;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

internal static class ConsumerTestData
{
    private static readonly Faker Faker = new();

    public static string Email() => Faker.Internet.Email().ToLowerInvariant();

    public static string FirstName() => Faker.Name.FirstName();

    public static string LastName() => Faker.Name.LastName();

    public static string Otp() => Faker.Random.ReplaceNumbers("######");

    public static string IpAddress() => Faker.Internet.Ip();

    public static string UserAgent() => Faker.Internet.UserAgent();

    public static string PhoneNumber() => Faker.Phone.PhoneNumber();
}
