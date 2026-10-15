using System.Text.Json;

namespace BLVK.Pickly.Logistics.Application.Common.Pagination;

public static class CursorPagination
{
    public static (DateTimeOffset? CreatedAtUtc, Guid? EntityId) Decode(
        string? cursor,
        string? expectedScope = null)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return (null, null);
        }

        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var payload = JsonSerializer.Deserialize<CursorPaginationPayload>(bytes);
            if (payload is null ||
                payload.EntityId == Guid.Empty ||
                (expectedScope is not null && !string.Equals(payload.Scope, expectedScope, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Invalid cursor.");
            }

            return (payload.CreatedAtUtc.ToUniversalTime(), payload.EntityId);
        }
        catch (Exception exception) when (exception is FormatException or JsonException or InvalidOperationException)
        {
            throw new InvalidOperationException("Invalid cursor.", exception);
        }
    }

    public static string Encode(DateTimeOffset createdAtUtc, Guid entityId, string? scope = null)
    {
        var payload = new CursorPaginationPayload(createdAtUtc.ToUniversalTime(), entityId, scope);
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        return Convert.ToBase64String(json);
    }
}
