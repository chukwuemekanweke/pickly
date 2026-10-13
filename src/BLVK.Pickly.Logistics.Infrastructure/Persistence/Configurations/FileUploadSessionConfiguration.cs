using BLVK.Pickly.Logistics.Domain.Common.FileUploads.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class FileUploadSessionConfiguration : IEntityTypeConfiguration<FileUploadSession>
{
    public void Configure(EntityTypeBuilder<FileUploadSession> builder)
    {
        builder.ToTable("FileUploadSessions", SchemaNames.Infrastructure);
        builder.HasKey(upload => upload.Id);

        builder.Property(upload => upload.TenantId).IsRequired();
        builder.Property(upload => upload.OwnerType).HasMaxLength(100).IsRequired();
        builder.Property(upload => upload.OwnerId).IsRequired();
        builder.Property(upload => upload.InitiatedByStakeholderId);
        builder.Property(upload => upload.Purpose).HasMaxLength(100).IsRequired();
        builder.Property(upload => upload.PolicyKey).HasMaxLength(100).IsRequired();
        builder.Property(upload => upload.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(upload => upload.ExpectedContentType).HasMaxLength(100).IsRequired();
        builder.Property(upload => upload.ExpectedContentLength).IsRequired();
        builder.Property(upload => upload.FileExtension).HasMaxLength(10).IsRequired();
        builder.Property(upload => upload.QuarantineObjectKey).HasMaxLength(1024).IsRequired();
        builder.Property(upload => upload.FinalObjectKey).HasMaxLength(1024).IsRequired();
        builder.Property(upload => upload.DestinationVisibility).IsRequired();
        builder.Property(upload => upload.FinalLocation).HasMaxLength(2048);
        builder.Property(upload => upload.ValidatedETag).HasMaxLength(255);
        builder.Property(upload => upload.Status).IsRequired();
        builder.Property(upload => upload.ExpiresAtUtc).IsRequired();
        builder.Property(upload => upload.RejectionReason).HasMaxLength(100);

        builder.HasIndex(upload => new { upload.TenantId, upload.OwnerType, upload.OwnerId, upload.Purpose });
        builder.HasIndex(upload => new { upload.Status, upload.ExpiresAtUtc });
    }
}
