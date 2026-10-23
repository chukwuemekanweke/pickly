using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsSnapshot;
using BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsTimeSeries;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Operations.Metrics;

/// <summary>
/// Public, aggregate-only operations metrics (counts, no identities) backing the operations dashboard page.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Tags("Operations Metrics")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route(EndpointUrl.OperationsMetrics.Route)]
public sealed class OperationsMetricsController(
    GetOperationsSnapshotHandler snapshotHandler,
    GetOperationsTimeSeriesHandler timeSeriesHandler,
    OperationsSnapshotRequestValidator snapshotValidator,
    OperationsTimeSeriesRequestValidator timeSeriesValidator) : ControllerBase
{
    [HttpGet("snapshot")]
    [ProducesResponseType<OperationsSnapshotResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSnapshot(
        [FromQuery] OperationsSnapshotRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await snapshotValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await snapshotHandler.HandleAsync(new(request.Date), cancellationToken);
        return result.Status switch
        {
            GetOperationsSnapshotStatus.Success => Ok(result.Snapshot),
            _ => Problem(400, "date_in_future", "Date in future", "Metrics are only available up to the current UTC day.")
        };
    }

    [HttpGet("time-series")]
    [ProducesResponseType<OperationsTimeSeriesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTimeSeries(
        [FromQuery] OperationsTimeSeriesRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await timeSeriesValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await timeSeriesHandler.HandleAsync(new(request.From, request.To), cancellationToken);
        return result.Status switch
        {
            GetOperationsTimeSeriesStatus.Success => Ok(result.TimeSeries),
            _ => Problem(
                400,
                "invalid_range",
                "Invalid date range",
                $"The range must cover between 1 and {GetOperationsTimeSeriesHandler.MaximumDays} days.")
        };
    }

    private static ObjectResult Problem(int status, string code, string title, string detail) =>
        AuthenticationProblemDetails.Create(status, code, title, detail);
}
