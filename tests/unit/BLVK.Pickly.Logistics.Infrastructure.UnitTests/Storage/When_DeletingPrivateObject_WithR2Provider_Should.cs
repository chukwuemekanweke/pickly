using NSubstitute;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Storage;

public sealed class When_DeletingPrivateObject_WithR2Provider_Should
{
    [Fact]
    public async Task TargetPrivateBucketAndScopedQuarantineKey()
    {
        var context = new CloudflareR2ProviderTestContext();

        await context.CreateProvider().DeletePrivateObjectAsync("quarantine/upload.png", CancellationToken.None);

        await context.Client.Received(1).DeleteObjectAsync(
            "private-bucket",
            "backend-template/quarantine/upload.png",
            Arg.Any<CancellationToken>());
    }
}
