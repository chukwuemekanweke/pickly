namespace BLVK.Pickly.Logistics.Infrastructure.Deliveries;

internal sealed record RoutingWindowContext(string Id, string ScheduleVersion, DateOnly LocalDate, DateTimeOffset EndsAtUtc);

internal static class RoutingWindowResolver
{
    public static RoutingWindowContext Resolve(DateTimeOffset departureAtUtc, DeliveryRoutingOptions options)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(options.ServiceAreaTimeZone);
        var local = TimeZoneInfo.ConvertTime(departureAtUtc, zone);
        var time = local.TimeOfDay;
        foreach (var window in options.Windows)
        {
            var sameDay = window.Start < window.End && window.ApplicableDays.Contains(local.DayOfWeek) && time >= window.Start && time < window.End;
            var overnightStart = window.Start > window.End && window.ApplicableDays.Contains(local.DayOfWeek) && time >= window.Start;
            var previous = local.DayOfWeek == DayOfWeek.Sunday ? DayOfWeek.Saturday : (DayOfWeek)((int)local.DayOfWeek - 1);
            var overnightEnd = window.Start > window.End && window.ApplicableDays.Contains(previous) && time < window.End;
            if (!sameDay && !overnightStart && !overnightEnd) continue;

            var endDate = overnightStart ? local.Date.AddDays(1) : local.Date;
            var localEnd = DateTime.SpecifyKind(endDate + window.End, DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(localEnd)) localEnd = localEnd.AddHours(1);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, zone);
            var applicableDate = overnightEnd ? DateOnly.FromDateTime(local.Date.AddDays(-1)) : DateOnly.FromDateTime(local.Date);
            return new(window.Id, options.ScheduleVersion, applicableDate, new DateTimeOffset(endUtc, TimeSpan.Zero));
        }
        throw new InvalidOperationException("No routing window covers the requested departure time.");
    }
}
