using BLVK.Pickly.Logistics.Application.Common.Pagination;
using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.LoginActivity;

public sealed class GetLoginActivityValidator : AbstractValidator<GetLoginActivityRequest>
{
    public GetLoginActivityValidator()
    {
        RuleFor(request => request.Limit)
            .InclusiveBetween(1, 100);
        RuleFor(request => request.Cursor)
            .Must(BeAValidCursor)
            .WithMessage("Cursor is invalid.");
    }

    private static bool BeAValidCursor(string? cursor)
    {
        try
        {
            CursorPagination.Decode(cursor);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
