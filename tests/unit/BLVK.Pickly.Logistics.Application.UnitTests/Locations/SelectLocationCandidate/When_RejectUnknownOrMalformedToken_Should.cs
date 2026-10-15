using BLVK.Pickly.Logistics.Application.Locations.Features.SelectLocationCandidate;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SelectLocationCandidate;

public sealed partial class When_SelectingLocationCandidate_WithSelectionToken_Should
{
[Theory]
    [InlineData("")]
    [InlineData("not-a-token")]
    [InlineData("0123456789abcdef0123456789abcdef.0")]
    [InlineData("0123456789ABCDEF0123456789ABCDEF.0")]
    public async Task RejectUnknownOrMalformedToken(string token)
    {
        var context = new LocationResolutionTestContext();

        var result = await context.SelectHandler().HandleAsync(
            new SelectLocationCandidateCommand(token, context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(SelectLocationCandidateStatus.SelectionNotFound);
    }
}
