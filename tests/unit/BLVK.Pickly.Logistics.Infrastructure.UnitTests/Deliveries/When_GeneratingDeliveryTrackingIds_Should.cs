using BLVK.Pickly.Logistics.Infrastructure.Deliveries;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Deliveries;

public sealed class When_GeneratingDeliveryTrackingIds_Should
{
    [Fact]
    public void ProduceCustomerFacingNonSequentialValues()
    {
        var generator = new DeliveryTrackingIdGenerator();

        var values = Enumerable.Range(0, 1000).Select(_ => generator.Generate()).ToArray();

        values.ShouldAllBe(value => value.Length == 12 && value.StartsWith("PKL-", StringComparison.Ordinal));
        values.Distinct(StringComparer.Ordinal).Count().ShouldBe(values.Length);
        values.ShouldAllBe(value => value.Substring(4).All(character =>
            "23456789ABCDEFGHJKLMNPQRSTUVWXYZ".Contains(character, StringComparison.Ordinal)));
    }
}
