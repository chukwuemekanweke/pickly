using Bogus;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication;

internal static class AuthenticationTestData
{
    private static readonly Faker Faker = new();

    public static string Email() => Faker.Internet.Email().ToLowerInvariant();

    public static string FirstName() => Faker.Name.FirstName();

    public static string LastName() => Faker.Name.LastName();

    public static string StrongPassword() => $"Aa1!{Faker.Random.Replace("????????")}";

    public static string WeakPassword() => Faker.Random.Replace("????????");

    public static string Otp() => Faker.Random.ReplaceNumbers("######");

    public static string IpAddress() => Faker.Internet.Ip();

    public static string UserAgent() => Faker.Internet.UserAgent();
}
