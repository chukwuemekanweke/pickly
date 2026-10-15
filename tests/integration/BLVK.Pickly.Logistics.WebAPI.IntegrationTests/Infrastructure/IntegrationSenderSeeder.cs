using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Creates verified sender stakeholders (with their own tenant, type and login) for integration scenarios and removes
/// exactly those records afterwards.
/// </summary>
public static class IntegrationSenderSeeder
{
    public const string Password = "P@ssw0rd123!";

    public static async Task<SeededSender> CreateVerifiedSenderAsync(IServiceProvider services, Guid tenantId)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var identityService = provider.GetRequiredService<IAuthenticationIdentityService>();
        var countryReadRepository = provider.GetRequiredService<IReadRepository<Country>>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var email = WebApiIntegrationTestData.Email();

        Guid? createdCountryId = null;
        var country = (await countryReadRepository.ListAsync(new FirstCountrySpecification())).FirstOrDefault();
        if (country is null)
        {
            country = Country.Create("Location Test Country", "LX", "+998", "https://example.com/lx.svg");
            await provider.GetRequiredService<IRepository<Country>>().AddAsync(country);
            createdCountryId = country.Id;
        }

        var user = AppUser.Create(email, "Ada", "Lovelace");
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var suffix = Guid.CreateVersion7().ToString("N");
        var tenant = Tenant.Create(tenantId, "Location Test", $"locations-{suffix}");
        var stakeholderType = StakeholderType.Create(tenantId, "Sender", $"sender-{suffix}");
        var stakeholder = Stakeholder.Create(user.Id, tenantId, country.Id, stakeholderType.Id, "Ada", "Lovelace");
        stakeholder.MarkVerified();

        await provider.GetRequiredService<IRepository<Tenant>>().AddAsync(tenant);
        await provider.GetRequiredService<IRepository<StakeholderType>>().AddAsync(stakeholderType);
        await provider.GetRequiredService<IRepository<Stakeholder>>().AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();

        return new SeededSender(email, tenantId, stakeholder.Id, stakeholderType.Id, createdCountryId);
    }

    public static async Task DeleteAsync(IServiceProvider services, SeededSender sender)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();

        await dbContext.SavedPickupLocations
            .IgnoreQueryFilters()
            .Where(location => location.SenderId == sender.StakeholderId)
            .ExecuteDeleteAsync();

        (await dbContext.Stakeholders.FirstOrDefaultAsync(item => item.Id == sender.StakeholderId))?.SetDeletedAudit(now, "test-cleanup");
        (await dbContext.StakeholderTypes.FirstOrDefaultAsync(item => item.Id == sender.StakeholderTypeId))?.SetDeletedAudit(now, "test-cleanup");
        (await dbContext.Tenants.FirstOrDefaultAsync(item => item.Id == sender.TenantId))?.SetDeletedAudit(now, "test-cleanup");
        var user = await userRepository.GetByEmailAsync(sender.Email);
        if (user is not null)
        {
            user.SetDeletedAudit(now, "test-cleanup");
            dbContext.Users.Update(user);
        }

        if (sender.CreatedCountryId.HasValue)
        {
            (await dbContext.Countries.FirstOrDefaultAsync(item => item.Id == sender.CreatedCountryId.Value))?.SetDeletedAudit(now, "test-cleanup");
        }

        await dbContext.SaveChangesAsync();
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification() => ApplyPaging(0, 1);
    }
}
