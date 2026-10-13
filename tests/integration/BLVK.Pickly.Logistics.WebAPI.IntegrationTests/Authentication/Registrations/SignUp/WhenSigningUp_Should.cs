using BLVK.Pickly.Logistics.Application.Authentication.Features.SignUp;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenSigningUp_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private bool _createdCountryForTest;
    private SignUpRequest _request = default!;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        _countryId = await ResolveCountryIdAsync();
        await EnsureDefaultStakeholderTypeExistsAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteAuthenticationRecordsAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnAcceptedAndQueueUserCreatedEvent()
    {
        GivenANewEmailAddress();
        await WhenSigningUp();
        await ThenTheRequestIsAcceptedAndUserCreatedEventIsQueued();

        void GivenANewEmailAddress()
        {
            _email = WebApiIntegrationTestData.Email();
            _request = new SignUpRequest(
                _email,
                Password,
                Password,
                _countryId,
                WebApiIntegrationTestData.FirstName(),
                WebApiIntegrationTestData.LastName(),
                "fr");
        }

        async Task WhenSigningUp()
        {
            _response = await Client.PostAsJsonAsync(EndpointUrl.Registrations.V1, _request);
        }

        async Task ThenTheRequestIsAcceptedAndUserCreatedEventIsQueued()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            var response = await _response.Content.ReadFromJsonAsync<SignUpResponse>();
            response.ShouldNotBeNull();
            response.RetryAtUtc.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
            response.RetryAtUtc.ShouldBeLessThanOrEqualTo(
                DateTimeOffset.UtcNow.Add(AuthenticationOtpDefaults.EmailConfirmationLifetime));

            using var scope = CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage>>();
            var outboxMessages = await repository.ListAsync(new UserCreatedOutboxMessagesSpecification());

            outboxMessages.Count.ShouldBe(1);
            outboxMessages[0].SentAtUtc.ShouldBeNull();
            var userCreated = JsonSerializer.Deserialize<UserCreated>(
                outboxMessages[0].Payload,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            userCreated.ShouldNotBeNull();
            userCreated.ExpiresAtUtc.ShouldBe(response.RetryAtUtc);
            userCreated.ExpiresAtUtc.ShouldBe(
                userCreated.RequestedAtUtc.Add(AuthenticationOtpDefaults.EmailConfirmationLifetime));

            var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
            var stakeholder = await stakeholderRepository.GetByIdAsync(userCreated.StakeholderId!.Value);
            stakeholder.ShouldNotBeNull();
            stakeholder.Language.ShouldBe("fr");
        }
    }

    private async Task DeleteAuthenticationRecordsAsync()
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            return;
        }

        using var scope = CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await userRepository.GetByEmailAsync(_email);

        if (user is not null)
        {
            var stakeholders = await stakeholderRepository.ListAsync(new StakeholderByAppUserIdCleanupSpecification(user.Id));
            foreach (var stakeholder in stakeholders)
            {
                stakeholderRepository.Remove(stakeholder);
            }
        }

        if (user is not null)
        {
            userRepository.Remove(user);
        }

        var outboxMessages = await outboxRepository.ListAsync(new UserCreatedOutboxMessagesSpecification());
        foreach (var outboxMessage in outboxMessages)
        {
            outboxRepository.Remove(outboxMessage);
        }

        var stakeholderTypes = await stakeholderTypeRepository.ListAsync(new DefaultCustomerStakeholderTypeCleanupSpecification(_tenantId));
        foreach (var stakeholderType in stakeholderTypes)
        {
            stakeholderTypeRepository.Remove(stakeholderType);
        }

        if (_createdCountryForTest)
        {
            var country = await countryRepository.GetByIdAsync(_countryId);
            if (country is not null)
            {
                countryRepository.Remove(country);
            }
        }

        if (user is not null || outboxMessages.Count > 0 || stakeholderTypes.Count > 0 || _createdCountryForTest)
        {
            await unitOfWork.SaveChangesAsync();
        }
    }

    private async Task<Guid> ResolveCountryIdAsync()
    {
        using var scope = CreateScope();
        var countryReadRepository = scope.ServiceProvider.GetRequiredService<IReadRepository<Country>>();
        var countryWriteRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var countries = await countryReadRepository.ListAsync(new FirstCountrySpecification());
        if (countries.Count > 0)
        {
            return countries[0].Id;
        }

        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        var country = Country.Create("Default Country", "DF", "+0", "https://example.com/flag.svg");
        await countryWriteRepository.AddAsync(country);
        await unitOfWork.SaveChangesAsync();
        _createdCountryForTest = true;

        return country.Id;
    }

    private async Task EnsureDefaultStakeholderTypeExistsAsync()
    {
        using var scope = CreateScope();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer");

        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await unitOfWork.SaveChangesAsync();
    }

    private sealed class UserCreatedOutboxMessagesSpecification : Specification<OutboxMessage>
    {
        public UserCreatedOutboxMessagesSpecification()
        {
            Where(message =>
                message.Kind == OutboxMessageKind.Event &&
                message.Type == typeof(UserCreated).FullName!);
        }
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification()
        {
            ApplyPaging(0, 1);
        }
    }

    private sealed class DefaultCustomerStakeholderTypeCleanupSpecification : Specification<StakeholderType>
    {
        public DefaultCustomerStakeholderTypeCleanupSpecification(Guid tenantId)
        {
            Where(stakeholderType => stakeholderType.TenantId == tenantId && stakeholderType.Key == "customer");
        }
    }

    private sealed class StakeholderByAppUserIdCleanupSpecification : Specification<Stakeholder>
    {
        public StakeholderByAppUserIdCleanupSpecification(Guid appUserId)
        {
            Where(stakeholder => stakeholder.AppUserId == appUserId);
        }
    }
}





