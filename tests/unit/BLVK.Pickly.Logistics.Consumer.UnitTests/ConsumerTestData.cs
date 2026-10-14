using BLVK.Pickly.Logistics.Domain.Common.Authentication;
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

    public static IRegistrationPolicyProvider RegistrationPolicyProvider(bool isMvp = false)
    {
        var provider = Substitute.For<IRegistrationPolicyProvider>();
        provider.GetAsync().Returns(RegistrationPolicy.Create(isMvp));
        return provider;
    }
}
