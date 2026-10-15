using BLVK.Pickly.Logistics.Application.Common.Pagination;
using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;

public sealed class ListPickupLocationsRequestValidator : AbstractValidator<ListPickupLocationsRequest>
{
    private const string CursorScope = "pickup-locations-v1";

    public ListPickupLocationsRequestValidator()
    {
        RuleFor(request => request.Limit).InclusiveBetween(1, 100);
        RuleFor(request => request.Cursor)
            .Must(BeValidCursor)
            .When(request => !string.IsNullOrWhiteSpace(request.Cursor))
            .WithMessage("Cursor is invalid or incompatible with pickup locations.");
    }

    private static bool BeValidCursor(string? cursor)
    {
        try
        {
            CursorPagination.Decode(cursor, CursorScope);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
