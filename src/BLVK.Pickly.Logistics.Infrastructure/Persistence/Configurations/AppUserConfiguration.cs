using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users", SchemaNames.Authentication);
        builder.Property(user => user.UserName).HasMaxLength(256);
        builder.Property(user => user.NormalizedUserName).HasMaxLength(256);
        builder.Property(user => user.Email).HasMaxLength(256);
        builder.Property(user => user.NormalizedEmail).HasMaxLength(256);

        builder.HasIndex(user => user.NormalizedUserName)
            .HasDatabaseName("UserNameIndex")
            .IsUnique()
            .HasFilter("\"NormalizedUserName\" IS NOT NULL AND \"IsDeleted\" = FALSE");
    }
}
