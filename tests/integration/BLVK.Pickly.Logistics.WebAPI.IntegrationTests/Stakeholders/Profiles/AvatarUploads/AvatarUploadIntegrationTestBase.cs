using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.FileUploads.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Storage;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using WireMock.Server;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Stakeholders.Profiles.AvatarUploads;

public abstract class AvatarUploadIntegrationTestBase : IAsyncLifetime
{
    protected const string ApplicationFolder = "integration-tests";
    protected const string PrivateBucketName = "private-avatars";
    protected const string PublicBucketName = "public-avatars";

    private const string Password = "P@ssw0rd123!";
    private readonly List<Guid> _uploadIds = [];
    private readonly CustomWebApplicationFactory _factory;
    private string _email = string.Empty;
    private Guid _countryId;
    private Guid _providerId;
    private Guid _stakeholderTypeId;
    private bool _createdCountryForTest;

    protected AvatarUploadIntegrationTestBase(ContainersFixture fixture)
    {
        StorageServer = WireMockServer.Start();
        _factory = new CustomWebApplicationFactory(
            fixture.PostgresConnectionString,
            fixture.RedisConnectionString,
            configurationOverrides: new Dictionary<string, string?>
            {
                ["ObjectStorage:CloudflareR2:Endpoint"] = StorageServer.Urls.Single(),
                ["ObjectStorage:CloudflareR2:ApplicationFolder"] = ApplicationFolder,
                ["ObjectStorage:CloudflareR2:PublicBucketName"] = PublicBucketName,
                ["ObjectStorage:CloudflareR2:PrivateBucketName"] = PrivateBucketName,
                ["ObjectStorage:CloudflareR2:AccessKeyId"] = "integration-access-key",
                ["ObjectStorage:CloudflareR2:SecretAccessKey"] = "integration-secret-key",
                ["ObjectStorage:CloudflareR2:PublicBaseUrl"] = "https://cdn.integration.invalid"
            });
    }

    protected HttpClient Client { get; private set; } = default!;
    protected WireMockServer StorageServer { get; }
    protected Guid StakeholderId { get; private set; }
    protected Guid TenantId { get; private set; }

    public async Task InitializeAsync()
    {
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        TenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", TenantId.ToString());
        _countryId = await ResolveCountryIdAsync();
        await CreateVerifiedUserAsync();
        await SeedFileStorageProviderAsync();
        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        await DeleteSeedDataAsync();
        Client.Dispose();
        await _factory.DisposeAsync();
        StorageServer.Dispose();
    }

    protected IServiceScope CreateScope() => _factory.Services.CreateScope();

    protected void TrackUpload(Guid uploadId) => _uploadIds.Add(uploadId);

    protected static string BuildPrivatePath(string objectKey) =>
        $"/{PrivateBucketName}/{ApplicationFolder}/{objectKey}";

    protected static string BuildPublicPath(string objectKey) =>
        $"/{PublicBucketName}/{ApplicationFolder}/{objectKey}";

    private async Task AuthenticateAsync()
    {
        using var response = await Client.PostAsJsonAsync(EndpointUrl.Sessions.V1, new SignInRequest(_email, Password));
        var payload = await response.Content.ReadFromJsonAsync<SignInResponse>();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }

    private async Task CreateVerifiedUserAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var firstName = WebApiIntegrationTestData.FirstName();
        var lastName = WebApiIntegrationTestData.LastName();
        _email = WebApiIntegrationTestData.Email();
        var user = AppUser.Create(_email, firstName, lastName);
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();
        var stakeholderType = StakeholderType.Create(TenantId, "Customer", "customer");
        var stakeholder = Stakeholder.Create(user.Id, TenantId, _countryId, stakeholderType.Id, firstName, lastName);
        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();
        StakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    private async Task SeedFileStorageProviderAsync()
    {
        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Provider>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var provider = Provider.Create(
            ProviderType.FileStorage,
            "Cloudflare R2",
            ObjectStorageProviderKeys.CloudflareR2,
            true);
        _providerId = provider.Id;
        await repository.AddAsync(provider);
        await unitOfWork.SaveChangesAsync();
    }

    private async Task<Guid> ResolveCountryIdAsync()
    {
        using var scope = CreateScope();
        var readRepository = scope.ServiceProvider.GetRequiredService<IReadRepository<Country>>();
        var writeRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var existing = await readRepository.ListAsync(new FirstCountrySpecification());
        if (existing.Count > 0)
        {
            return existing[0].Id;
        }

        var country = Country.Create("Default Country", "DF", "+0", "https://example.com/flag.svg");
        await writeRepository.AddAsync(country);
        await unitOfWork.SaveChangesAsync();
        _createdCountryForTest = true;
        return country.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var uploadRepository = scope.ServiceProvider.GetRequiredService<IRepository<FileUploadSession>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var providerRepository = scope.ServiceProvider.GetRequiredService<IRepository<Provider>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        foreach (var uploadId in _uploadIds)
        {
            var uploadIdText = uploadId.ToString();
            var messages = await dbContext.OutboxMessages
                .Where(message => message.Payload.Contains(uploadIdText))
                .ToListAsync();
            dbContext.OutboxMessages.RemoveRange(messages);
            var upload = await uploadRepository.GetByIdAsync(uploadId);
            if (upload is not null)
            {
                uploadRepository.Remove(upload);
            }
        }

        var provider = await providerRepository.GetByIdAsync(_providerId);
        if (provider is not null)
        {
            providerRepository.Remove(provider);
        }

        var stakeholder = await stakeholderRepository.GetByIdAsync(StakeholderId);
        if (stakeholder is not null)
        {
            stakeholderRepository.Remove(stakeholder);
        }

        var stakeholderType = await stakeholderTypeRepository.GetByIdAsync(_stakeholderTypeId);
        if (stakeholderType is not null)
        {
            stakeholderTypeRepository.Remove(stakeholderType);
        }

        var user = await userRepository.GetByEmailAsync(_email);
        if (user is not null)
        {
            userRepository.Remove(user);
        }

        if (_createdCountryForTest)
        {
            var country = await countryRepository.GetByIdAsync(_countryId);
            if (country is not null)
            {
                countryRepository.Remove(country);
            }
        }

        await unitOfWork.SaveChangesAsync();
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification() => ApplyPaging(0, 1);
    }
}
