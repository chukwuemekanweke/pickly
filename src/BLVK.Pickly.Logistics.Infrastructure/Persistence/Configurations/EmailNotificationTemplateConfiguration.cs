using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class EmailNotificationTemplateConfiguration : IEntityTypeConfiguration<EmailNotificationTemplate>
{
    public void Configure(EntityTypeBuilder<EmailNotificationTemplate> builder)
    {
        builder.ToTable("EmailNotificationTemplates", SchemaNames.Notifications);

        builder.HasKey(template => template.Id);

        builder.Property(template => template.NotificationType)
            .IsRequired();

        builder.Property(template => template.Language)
            .HasMaxLength(35)
            .IsRequired();

        builder.Property(template => template.Description)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(template => template.Subject)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(template => template.TemplateFileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.HasIndex(template => new { template.NotificationType, template.Language })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
