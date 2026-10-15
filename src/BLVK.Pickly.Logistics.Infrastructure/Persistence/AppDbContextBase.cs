using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.FileUploads.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public abstract class AppDbContextBase<TContext>(DbContextOptions<TContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
    where TContext : DbContext
{
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<CountryCurrency> CountryCurrencies => Set<CountryCurrency>();
    public DbSet<MessageInbox> MessageInboxes => Set<MessageInbox>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<PaymentProvider> PaymentProviders => Set<PaymentProvider>();
    public DbSet<PaymentProviderConfiguration> PaymentProviderConfigurations => Set<PaymentProviderConfiguration>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentWebhookInbox> PaymentWebhookInboxes => Set<PaymentWebhookInbox>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<SubscriptionActivation> SubscriptionActivations => Set<SubscriptionActivation>();
    public DbSet<EmailNotificationTemplate> EmailNotificationTemplates => Set<EmailNotificationTemplate>();
    public DbSet<EmailNotificationLog> EmailNotificationLogs => Set<EmailNotificationLog>();
    public DbSet<EmailDeliveryWebhookInbox> EmailDeliveryWebhookInboxes => Set<EmailDeliveryWebhookInbox>();
    public DbSet<TenantEmailBaseTemplate> TenantEmailBaseTemplates => Set<TenantEmailBaseTemplate>();
    public DbSet<Stakeholder> Stakeholders => Set<Stakeholder>();
    public DbSet<FileUploadSession> FileUploadSessions => Set<FileUploadSession>();
    public DbSet<StakeholderType> StakeholderTypes => Set<StakeholderType>();
    public DbSet<AuthenticationRefreshToken> AuthenticationRefreshTokens => Set<AuthenticationRefreshToken>();
    public DbSet<AuthenticationSession> AuthenticationSessions => Set<AuthenticationSession>();
    public DbSet<IpAddress> IpAddresses => Set<IpAddress>();
    public DbSet<IpAddressLocation> IpAddressLocations => Set<IpAddressLocation>();
    public DbSet<LoginActivity> LoginActivities => Set<LoginActivity>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryItem> DeliveryItems => Set<DeliveryItem>();
    public DbSet<DeliveryStatusHistory> DeliveryStatusHistory => Set<DeliveryStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", SchemaNames.Authentication);
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", SchemaNames.Authentication);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplyAuditingConventions(modelBuilder);
        ApplySoftDeleteFilters(modelBuilder);
    }

    private static void ApplyAuditingConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(type => type.ClrType is not null && !type.IsOwned()))
        {
            if (typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Property<string>(nameof(IAuditableEntity.CreatedBy)).HasMaxLength(200);
                modelBuilder.Entity(entityType.ClrType).Property<string>(nameof(IAuditableEntity.UpdatedBy)).HasMaxLength(200);
            }

            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Property<string>(nameof(ISoftDelete.DeletedBy)).HasMaxLength(200);
            }
        }
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(type => type.ClrType is not null && !type.IsOwned()))
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeletedProperty = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                [typeof(bool)],
                parameter,
                Expression.Constant(nameof(ISoftDelete.IsDeleted)));
            var compareExpression = Expression.Equal(isDeletedProperty, Expression.Constant(false));
            var lambda = Expression.Lambda(compareExpression, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
