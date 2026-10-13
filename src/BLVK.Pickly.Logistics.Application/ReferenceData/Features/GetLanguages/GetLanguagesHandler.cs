using BLVK.Pickly.Logistics.Domain.Common.Localization;

namespace BLVK.Pickly.Logistics.Application.ReferenceData.Features.GetLanguages;

public sealed class GetLanguagesHandler
{
    public IReadOnlyList<GetLanguagesResponse> Handle() =>
    [
        new(SupportedLanguages.English, "English", "English"),
        new(SupportedLanguages.French, "French", "Français")
    ];
}
