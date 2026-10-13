using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class TenantEmailBaseTemplateConfiguration : IEntityTypeConfiguration<TenantEmailBaseTemplate>
{
    public void Configure(EntityTypeBuilder<TenantEmailBaseTemplate> builder)
    {
        builder.ToTable("TenantEmailBaseTemplates", SchemaNames.Notifications);

        builder.HasKey(template => template.Id);

        builder.Property(template => template.TenantId)
            .IsRequired();

        builder.Property(template => template.Description)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(template => template.HtmlTemplate)
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(template => template.TenantId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
