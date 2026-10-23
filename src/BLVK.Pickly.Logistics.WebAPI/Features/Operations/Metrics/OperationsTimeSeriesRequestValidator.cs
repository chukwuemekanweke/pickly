using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Operations.Metrics;

public sealed class OperationsTimeSeriesRequestValidator : AbstractValidator<OperationsTimeSeriesRequest>
{
    public OperationsTimeSeriesRequestValidator()
    {
        RuleFor(request => request.From)
            .GreaterThanOrEqualTo(OperationsMetricsRequestLimits.EarliestDate)
            .When(request => request.From.HasValue);
        RuleFor(request => request.To)
            .GreaterThanOrEqualTo(OperationsMetricsRequestLimits.EarliestDate)
            .When(request => request.To.HasValue);
        RuleFor(request => request.To)
            .GreaterThanOrEqualTo(request => request.From)
            .When(request => request.From.HasValue && request.To.HasValue)
            .WithMessage("'To' must be on or after 'From'.");
    }
}
