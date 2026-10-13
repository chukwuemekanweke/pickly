namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenResponse<T>(
    int StatusCode,
    string Message,
    T Data);

public sealed record SafeHavenPaginatedResponse<T>(
    int StatusCode,
    string Message,
    IReadOnlyList<T> Data,
    SafeHavenPagination? Pagination = null);
