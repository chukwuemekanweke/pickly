using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessSafeHavenWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SafeHavenAccountCreditCommandData = BLVK.Pickly.Logistics.Application.Payments.Features.ProcessSafeHavenWebhook.SafeHavenAccountCreditWebhookData;
using SafeHavenAccountDebitCommandData = BLVK.Pickly.Logistics.Application.Payments.Features.ProcessSafeHavenWebhook.SafeHavenAccountDebitWebhookData;
using SafeHavenVirtualAccountTransferCommandData = BLVK.Pickly.Logistics.Application.Payments.Features.ProcessSafeHavenWebhook.SafeHavenVirtualAccountTransferWebhookData;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Payments.Webhooks.SafeHaven;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route(EndpointUrl.PaymentWebhooks.SafeHaven.Route)]
public sealed class SafeHavenWebhooksController(
    ProcessSafeHavenAccountCreditWebhookHandler accountCreditHandler,
    ProcessSafeHavenAccountDebitWebhookHandler accountDebitHandler,
    ProcessSafeHavenVirtualAccountTransferWebhookHandler virtualAccountTransferHandler) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Handle([FromBody] SafeHavenWebhookRequest request, CancellationToken cancellationToken)
    {
        var rawPayload = JsonSerializer.Serialize(request, JsonSerializerOptions);

        return request.Event switch
        {
            SafeHavenWebhookEvents.AccountCredit => await HandleAccountCreditAsync(
                request.Event,
                Map(
                    request.Data.Deserialize<SafeHavenAccountCreditWebhookData>(JsonSerializerOptions)
                    ?? throw new JsonException("Unable to deserialize SafeHaven account credit webhook payload.")),
                rawPayload,
                cancellationToken),
            SafeHavenWebhookEvents.AccountDebit => await HandleAccountDebitAsync(
                request.Event,
                Map(
                    request.Data.Deserialize<SafeHavenAccountDebitWebhookData>(JsonSerializerOptions)
                    ?? throw new JsonException("Unable to deserialize SafeHaven account debit webhook payload.")),
                rawPayload,
                cancellationToken),
            SafeHavenWebhookEvents.VirtualAccountTransfer => await HandleVirtualAccountTransferAsync(
                request.Event,
                Map(
                    request.Data.Deserialize<SafeHavenVirtualAccountTransferWebhookData>(JsonSerializerOptions)
                    ?? throw new JsonException("Unable to deserialize SafeHaven virtual account transfer webhook payload.")),
                rawPayload,
                cancellationToken),
            _ => throw new JsonException($"Unsupported SafeHaven webhook event '{request.Event}'.")
        };
    }

    private async Task<IActionResult> HandleAccountCreditAsync(
        string eventName,
        SafeHavenAccountCreditCommandData data,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        var command = new ProcessSafeHavenWebhookCommand<SafeHavenAccountCreditCommandData>(
            new SafeHavenWebhook<SafeHavenAccountCreditCommandData>(eventName, data),
            rawPayload);

        await accountCreditHandler.HandleAsync(command, cancellationToken);

        return Ok();
    }

    private async Task<IActionResult> HandleAccountDebitAsync(
        string eventName,
        SafeHavenAccountDebitCommandData data,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        var command = new ProcessSafeHavenWebhookCommand<SafeHavenAccountDebitCommandData>(
            new SafeHavenWebhook<SafeHavenAccountDebitCommandData>(eventName, data),
            rawPayload);

        await accountDebitHandler.HandleAsync(command, cancellationToken);

        return Ok();
    }

    private async Task<IActionResult> HandleVirtualAccountTransferAsync(
        string eventName,
        SafeHavenVirtualAccountTransferCommandData data,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        var command = new ProcessSafeHavenWebhookCommand<SafeHavenVirtualAccountTransferCommandData>(
            new SafeHavenWebhook<SafeHavenVirtualAccountTransferCommandData>(eventName, data),
            rawPayload);

        await virtualAccountTransferHandler.HandleAsync(command, cancellationToken);

        return Ok();
    }

    private static SafeHavenAccountCreditCommandData Map(SafeHavenAccountCreditWebhookData data) =>
        new(
            data.Queued,
            data.LimitExceeded,
            data.Id,
            data.Client,
            data.Account,
            data.TransactionType,
            data.SessionId,
            data.NameEnquiryReference,
            data.PaymentReference,
            data.MandateReference,
            data.IsReversed,
            data.ReversalReference,
            data.Provider,
            data.ProviderChannel,
            data.ProviderChannelCode,
            data.DestinationInstitutionCode,
            data.CreditAccountName,
            data.CreditAccountNumber,
            data.CreditBankVerificationNumber,
            data.CreditKycLevel,
            data.DebitAccountName,
            data.DebitAccountNumber,
            data.RealDebitAccountName,
            data.RealDebitAccountNumber,
            data.DebitBankVerificationNumber,
            data.DebitKycLevel,
            data.TransactionLocation,
            data.Narration,
            data.Amount,
            data.Fees,
            data.Vat,
            data.StampDuty,
            data.ResponseCode,
            data.ResponseMessage,
            data.Status,
            data.IsDeleted,
            data.CreatedAt,
            data.UpdatedAt);

    private static SafeHavenAccountDebitCommandData Map(SafeHavenAccountDebitWebhookData data) =>
        new(
            data.Queued,
            data.LimitExceeded,
            data.Id,
            data.Client,
            data.Account,
            data.TransactionType,
            data.SessionId,
            data.NameEnquiryReference,
            data.PaymentReference,
            data.MandateReference,
            data.IsReversed,
            data.ReversalReference,
            data.Provider,
            data.ProviderChannel,
            data.ProviderChannelCode,
            data.DestinationInstitutionCode,
            data.CreditAccountName,
            data.CreditAccountNumber,
            data.CreditBankVerificationNumber,
            data.CreditKycLevel,
            data.DebitAccountName,
            data.DebitAccountNumber,
            data.DebitBankVerificationNumber,
            data.DebitKycLevel,
            data.TransactionLocation,
            data.Narration,
            data.Amount,
            data.Fees,
            data.Vat,
            data.StampDuty,
            data.ResponseCode,
            data.ResponseMessage,
            data.Status,
            data.IsDeleted,
            data.CreatedAt,
            data.CreatedBy,
            data.UpdatedAt);

    private static SafeHavenVirtualAccountTransferCommandData Map(SafeHavenVirtualAccountTransferWebhookData data) =>
        new(
            data.Id,
            data.Client,
            data.VirtualAccount,
            data.SessionId,
            data.NameEnquiryReference,
            data.PaymentReference,
            data.IsReversed,
            data.ReversalReference,
            data.Provider,
            data.ProviderChannel,
            data.ProviderChannelCode,
            data.DestinationInstitutionCode,
            data.CreditAccountName,
            data.CreditAccountNumber,
            data.CreditBankVerificationNumber,
            data.CreditKycLevel,
            data.DebitAccountName,
            data.DebitAccountNumber,
            data.DebitBankVerificationNumber,
            data.DebitKycLevel,
            data.TransactionLocation,
            data.Narration,
            data.Amount,
            data.Fees,
            data.Vat,
            data.StampDuty,
            data.ResponseCode,
            data.ResponseMessage,
            data.Status,
            data.IsDeleted,
            data.CreatedAt,
            data.DeclinedAt,
            data.UpdatedAt);
}
