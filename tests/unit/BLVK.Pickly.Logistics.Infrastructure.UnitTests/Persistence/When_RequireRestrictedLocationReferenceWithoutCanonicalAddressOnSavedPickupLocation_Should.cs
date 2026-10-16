using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Persistence;

public sealed partial class When_ConfiguringLocationPersistence_Should
{
[Fact]
    public void RequireRestrictedLocationReferenceWithoutCanonicalAddressOnSavedPickupLocation()
    {
        using var dbContext = CreateContext();
        var savedType = dbContext.Model.FindEntityType(typeof(SavedPickupLocation))!;

        var foreignKey = savedType.GetForeignKeys().Single(key => key.PrincipalEntityType.ClrType == typeof(Location));
        foreignKey.Properties.Single().Name.ShouldBe(nameof(SavedPickupLocation.LocationId));
        foreignKey.IsRequired.ShouldBeTrue();
        foreignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
        var columns = savedType.GetProperties().Select(property => property.GetColumnName()).ToArray();
        columns.ShouldNotContain("Latitude");
        columns.ShouldNotContain("Longitude");
        columns.ShouldNotContain("AddressLine");
        savedType.GetNavigations().ShouldBeEmpty();
    }
}
