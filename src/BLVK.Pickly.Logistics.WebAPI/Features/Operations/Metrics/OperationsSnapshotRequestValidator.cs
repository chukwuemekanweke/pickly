using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Operations.Metrics;

public sealed class OperationsSnapshotRequestValidator : AbstractValidator<OperationsSnapshotRequest>
{
    public OperationsSnapshotRequestValidator()
    {
        RuleFor(request => request.Date)
            .GreaterThanOrEqualTo(OperationsMetricsRequestLimits.EarliestDate)
            .When(request => request.Date.HasValue);
    }
}
