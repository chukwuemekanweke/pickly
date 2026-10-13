using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class WhenApplyingSpecification_Should
{
    [Fact]
    public void FilterOrderAndPage()
    {
        const string firstEmail = "alpha@example.com";
        const string secondEmail = "bravo@example.com";
        const string thirdEmail = "charlie@example.com";
        var users = new[]
        {
            AppUser.Create(firstEmail),
            AppUser.Create(secondEmail),
            AppUser.Create(thirdEmail)
        }.AsQueryable();

        var specification = new OrderedPagedUsersSpecification();

        var result = SpecificationEvaluator.GetQuery(users, specification).ToList();

        var expectedEmails = users
            .OrderByDescending(user => user.Email)
            .Take(2)
            .Select(user => user.Email)
            .ToList();

        result.Select(user => user.Email).ShouldBe(expectedEmails);
    }

    private sealed class OrderedPagedUsersSpecification : Specification<AppUser>
    {
        public OrderedPagedUsersSpecification()
        {
            Where(user => user.Email != null && user.Email.Contains("@example.com"));
            ApplyOrderByDescending(user => user.Email!);
            ApplyPaging(0, 2);
        }
    }
}




