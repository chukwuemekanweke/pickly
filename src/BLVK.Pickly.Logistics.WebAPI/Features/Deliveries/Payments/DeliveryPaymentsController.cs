using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDeliveryPayments;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.InitiateDeliveryPayment;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Payments;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route("api/v{version:apiVersion}/deliveries/{deliveryId:guid}/payments")]
public sealed class DeliveryPaymentsController(
    InitiateDeliveryPaymentHandler initiateHandler,
    GetDeliveryPaymentsHandler getHandler,
    InitiateDeliveryPaymentRequestValidator validator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Initiate(
        Guid deliveryId,
        [FromBody] InitiateDeliveryPaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await initiateHandler.HandleAsync(
            new(deliveryId, request.PaymentProviderId, idempotencyKey, Actor()), cancellationToken);
        return result.Status switch
        {
            InitiateDeliveryPaymentStatus.Success => Ok(result),
            InitiateDeliveryPaymentStatus.NotAuthenticated => Unauthorized(),
            InitiateDeliveryPaymentStatus.NotFound => NotFound(),
            InitiateDeliveryPaymentStatus.AlreadyPaid => Problem(409, "delivery_already_paid", "Delivery already paid", "This delivery's payment obligation has already been satisfied."),
            InitiateDeliveryPaymentStatus.NotPayable => Problem(409, "delivery_not_payable", "Delivery is not payable", "This delivery cannot accept a prepaid digital payment."),
            InitiateDeliveryPaymentStatus.ProviderConflict => Problem(409, "active_payment_attempt", "Payment already in progress", "Another provider already has an active payment attempt for this delivery."),
            InitiateDeliveryPaymentStatus.PaymentRequiresReview => PaymentOutcomeProblem(
                result, "payment_requires_review", "Payment requires review", "A payment may have settled and must be reviewed before another attempt can be created."),
            InitiateDeliveryPaymentStatus.PaymentFailed => PaymentOutcomeProblem(
                result, "payment_failed", "Payment failed", "This idempotency key belongs to a failed payment attempt."),
            InitiateDeliveryPaymentStatus.PaymentExpired => PaymentOutcomeProblem(
                result, "payment_expired", "Payment expired", "This idempotency key belongs to an expired payment attempt."),
            InitiateDeliveryPaymentStatus.PaymentCancelled => PaymentOutcomeProblem(
                result, "payment_cancelled", "Payment cancelled", "This idempotency key belongs to a cancelled payment attempt."),
            _ => Problem(503, "payment_provider_unavailable", "Payment provider unavailable", "The payment attempt was reserved and can be retried safely.")
        };
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid deliveryId, CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(new(deliveryId, Actor()), cancellationToken);
        return result.Status switch
        {
            GetDeliveryPaymentsStatus.NotAuthenticated => Unauthorized(),
            GetDeliveryPaymentsStatus.NotFound => NotFound(),
            _ => Ok(result.Response)
        };
    }

    private ActorContext Actor() => Guid.TryParse(currentActor.ActorId, out var id)
        ? new(id, currentActor.TenantId, currentActor.CorrelationId, currentActor.FlowId)
        : ActorContext.FromAnonymousActor(currentActor);

    private static ObjectResult Problem(int status, string code, string title, string detail) =>
        AuthenticationProblemDetails.Create(status, code, title, detail);

    private static ObjectResult PaymentOutcomeProblem(
        InitiateDeliveryPaymentResult result,
        string code,
        string title,
        string detail) =>
        AuthenticationProblemDetails.Create(
            409,
            code,
            title,
            detail,
            new Dictionary<string, object?>
            {
                ["paymentTransactionId"] = result.PaymentTransactionId,
                ["merchantReference"] = result.MerchantReference,
                ["paymentStatus"] = result.PaymentStatus?.ToString(),
                ["paymentProviderId"] = result.PaymentProviderId,
                ["paymentProviderName"] = result.PaymentProviderName,
                ["isReplay"] = result.IsReplay
            });
}
