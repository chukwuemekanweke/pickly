using BLVK.Pickly.Logistics.Application.Authentication.Features.RequestEmailConfirmationOtp;
using BLVK.Pickly.Logistics.Contracts.Commands.Authentication;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailConfirmations;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Authentication.EmailConfirmations.RequestCode;

[Collection(nameof(ContainersCollection))]
public sealed class When_RequestingEmailConfirmationCode_WithNoActiveOtp_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private bool _createdCountryForTest;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        _countryId = await ResolveCountryIdAsync();
        await CreateUserWithStakeholderAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteAuthenticationRecordsAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task QueueEmailConfirmationCodeAndReturnRetryTime()
    {
        _response = await Client.PostAsJsonAsync(
            EndpointUrl.EmailConfirmations.ConfirmationCodeV1,
            new RequestEmailConfirmationOtpRequest(_email));

        _response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var response = await _response.Content.ReadFromJsonAsync<RequestEmailConfirmationOtpResponse>();
        response.ShouldNotBeNull();
        response.RetryAtUtc.ShouldBeGreaterThan(DateTimeOffset.UtcNow);

        using var scope = CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage>>();
        var messages = await outboxRepository.ListAsync(new EmailConfirmationOtpOutboxSpecification());
        messages.Count.ShouldBe(1);
        messages[0].SentAtUtc.ShouldBeNull();
        var command = JsonSerializer.Deserialize<SendEmailConfirmationOtpCommand>(
            messages[0].Payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        command.ShouldNotBeNull();
        command.ExpiresAtUtc.ShouldBe(response.RetryAtUtc);
        command.ExpiresAtUtc.ShouldBe(
            command.RequestedAt.Add(AuthenticationOtpDefaults.EmailConfirmationLifetime));
    }

    private async Task CreateUserWithStakeholderAsync()
    {
        _email = WebApiIntegrationTestData.Email();
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = AppUser.Create(_email);
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer");
        var stakeholder = Stakeholder.Create(
            user.Id,
            _tenantId,
            _countryId,
            stakeholderType.Id,
            WebApiIntegrationTestData.FirstName(),
            WebApiIntegrationTestData.LastName());
        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();
        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    private async Task DeleteAuthenticationRecordsAsync()
    {
        using var scope = CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await userRepository.GetByEmailAsync(_email);
        var stakeholders = await stakeholderRepository.ListAsync(new StakeholderByIdSpecification(_stakeholderId));
        var stakeholderTypes = await stakeholderTypeRepository.ListAsync(new StakeholderTypeByIdSpecification(_stakeholderTypeId));
        var messages = await outboxRepository.ListAsync(new EmailConfirmationOtpOutboxSpecification());

        foreach (var stakeholder in stakeholders)
        {
            stakeholderRepository.Remove(stakeholder);
        }

        foreach (var stakeholderType in stakeholderTypes)
        {
            stakeholderTypeRepository.Remove(stakeholderType);
        }

        foreach (var message in messages)
        {
            outboxRepository.Remove(message);
        }

        if (_createdCountryForTest)
        {
            var country = await countryRepository.GetByIdAsync(_countryId);
            if (country is not null)
            {
                countryRepository.Remove(country);
            }
        }

        if (user is not null)
        {
            userRepository.Remove(user);
        }

        await unitOfWork.SaveChangesAsync();
    }

    private async Task<Guid> ResolveCountryIdAsync()
    {
        using var scope = CreateScope();
        var readRepository = scope.ServiceProvider.GetRequiredService<IReadRepository<Country>>();
        var writeRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var countries = await readRepository.ListAsync(new FirstCountrySpecification());
        if (countries.Count > 0)
        {
            return countries[0].Id;
        }

        var country = Country.Create("Default Country", "DF", "+0", "https://example.com/flag.svg");
        await writeRepository.AddAsync(country);
        await unitOfWork.SaveChangesAsync();
        _createdCountryForTest = true;
        return country.Id;
    }

    private sealed class EmailConfirmationOtpOutboxSpecification : Specification<OutboxMessage>
    {
        public EmailConfirmationOtpOutboxSpecification() =>
            Where(message =>
                message.Kind == OutboxMessageKind.Command &&
                message.Type == typeof(SendEmailConfirmationOtpCommand).FullName!);
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification() => ApplyPaging(0, 1);
    }

    private sealed class StakeholderByIdSpecification : Specification<Stakeholder>
    {
        public StakeholderByIdSpecification(Guid stakeholderId) =>
            Where(stakeholder => stakeholder.Id == stakeholderId);
    }

    private sealed class StakeholderTypeByIdSpecification : Specification<StakeholderType>
    {
        public StakeholderTypeByIdSpecification(Guid stakeholderTypeId) =>
            Where(stakeholderType => stakeholderType.Id == stakeholderTypeId);
    }
}
