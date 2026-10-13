using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Application.Payments.Features.ActivatePaymentProvider;

public sealed class ActivatePaymentProviderHandler(
    IRepository<PaymentProvider> paymentProviderRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<ActivatePaymentProviderResult> HandleAsync(
        ActivatePaymentProviderCommand command,
        CancellationToken cancellationToken)
    {
        var paymentProvider = await paymentProviderRepository.GetByIdAsync(command.PaymentProviderId, cancellationToken);
        if (paymentProvider is null)
        {
            return new ActivatePaymentProviderResult(
                ActivatePaymentProviderStatus.ProviderNotFound,
                $"Payment provider '{command.PaymentProviderId}' was not found.");
        }

        if (paymentProvider.IsActive == command.IsActive)
        {
            return new ActivatePaymentProviderResult(ActivatePaymentProviderStatus.Success);
        }

        paymentProvider.SetActive(command.IsActive);
        paymentProviderRepository.Update(paymentProvider);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ActivatePaymentProviderResult(ActivatePaymentProviderStatus.Success);
    }
}
