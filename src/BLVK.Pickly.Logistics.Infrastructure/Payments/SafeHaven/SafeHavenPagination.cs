namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenPagination(
    int Total,
    int Pages,
    string Page,
    string Limit);
