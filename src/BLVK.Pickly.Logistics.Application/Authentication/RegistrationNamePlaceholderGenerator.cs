namespace BLVK.Pickly.Logistics.Application.Authentication;

public static class RegistrationNamePlaceholderGenerator
{
    public static string Generate() => $"pikly-{Guid.CreateVersion7():N}";
}
