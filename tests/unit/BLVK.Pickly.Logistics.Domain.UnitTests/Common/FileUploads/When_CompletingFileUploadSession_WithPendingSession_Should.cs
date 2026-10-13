using BLVK.Pickly.Logistics.Domain.Common.FileUploads.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Storage;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Common.FileUploads;

public sealed class When_CompletingFileUploadSession_WithPendingSession_Should
{
    [Fact]
    public void PreservePurposeAndRecordValidatedLocation()
    {
        var tenantId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var initiatedByStakeholderId = Guid.CreateVersion7();
        var session = FileUploadSession.Create(
            Guid.CreateVersion7(),
            tenantId,
            "kyc-application",
            ownerId,
            initiatedByStakeholderId,
            "kyc-identity-document",
            "kyc-pdf-v1",
            "identity.pdf",
            "application/pdf",
            128,
            ".pdf",
            "quarantine/identity.pdf",
            "documents/identity.pdf",
            ObjectStorageVisibility.Private,
            new DateTimeOffset(2026, 8, 16, 12, 10, 0, TimeSpan.Zero));

        session.MarkCompleted("https://storage.example/private/documents/identity.pdf", "etag-a");

        session.Status.ShouldBe(FileUploadStatus.Completed);
        session.TenantId.ShouldBe(tenantId);
        session.OwnerType.ShouldBe("kyc-application");
        session.OwnerId.ShouldBe(ownerId);
        session.InitiatedByStakeholderId.ShouldBe(initiatedByStakeholderId);
        session.Purpose.ShouldBe("kyc-identity-document");
        session.PolicyKey.ShouldBe("kyc-pdf-v1");
        session.DestinationVisibility.ShouldBe(ObjectStorageVisibility.Private);
        session.FinalLocation.ShouldBe("https://storage.example/private/documents/identity.pdf");
        session.ValidatedETag.ShouldBe("etag-a");
    }
}
