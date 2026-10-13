using System.Globalization;

namespace BLVK.Pickly.Logistics.Domain.Payments;

public sealed record WalletTransactionNarrativeTemplate(string Title, string DescriptionTemplate)
{
    public string CreateDescription(params object[] arguments) =>
        string.Format(CultureInfo.InvariantCulture, DescriptionTemplate, arguments);
}
