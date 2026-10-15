using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Persistence;

public sealed partial class When_ConfiguringLocationPersistence_Should
{
[Fact]
    public void EnforceUniqueProviderReferencesAndResolutionKeys()
    {
        using var dbContext = CreateContext();

        dbContext.Model.FindEntityType(typeof(LocationProviderReference))!.GetIndexes().ShouldContain(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(new[]
            {
                nameof(LocationProviderReference.Provider),
                nameof(LocationProviderReference.FeatureId)
            }));
        dbContext.Model.FindEntityType(typeof(LocationResolutionKey))!.GetIndexes().ShouldContain(index =>
            index.IsUnique && index.Properties.Single().Name == nameof(LocationResolutionKey.Key));
    }
}
