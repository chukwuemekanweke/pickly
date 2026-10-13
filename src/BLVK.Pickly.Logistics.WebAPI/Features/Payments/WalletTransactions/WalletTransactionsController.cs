using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;
using BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTransactions;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Payments.WalletTransactions;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.Payments.Route)]
public sealed class WalletTransactionsController(
    GetStakeholderWalletTransactionsHandler handler,
    GetStakeholderWalletTransactionsValidator validator,
    GetStakeholderWalletTopUpTransactionDetailHandler topUpDetailHandler,
    GetStakeholderWalletTopUpTransactionDetailValidator topUpDetailValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet("wallet-transactions")]
    [ProducesResponseType<GetStakeholderWalletTransactionsResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetStakeholderWalletTransactionsResult>> Handle(
        [FromQuery] GetStakeholderWalletTransactionsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await handler.HandleAsync(
            new GetStakeholderWalletTransactionsCommand(
                request.Limit,
                request.Cursor,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("wallet-transactions/top-ups/{walletTransactionId:guid}")]
    [ProducesResponseType<GetStakeholderWalletTopUpTransactionDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetStakeholderWalletTopUpTransactionDetailResponse>> GetTopUpDetail(
        [FromRoute] GetStakeholderWalletTopUpTransactionDetailRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await topUpDetailValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await topUpDetailHandler.HandleAsync(
            new GetStakeholderWalletTopUpTransactionDetailCommand(
                request.WalletTransactionId,
                ActorContext.FromCurrentActor(currentActor)),
            cancellationToken);

        return result.Status switch
        {
            GetStakeholderWalletTopUpTransactionDetailStatus.NotFound => NotFound(result.Error ?? "Wallet top-up transaction not found."),
            _ => Ok(result.Transaction)
        };
    }
}
