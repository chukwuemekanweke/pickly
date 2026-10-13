using Humanizer;
using System.Globalization;

namespace BLVK.Pickly.Logistics.Domain.Common.Formatting;

public static class DateTimeFormatter
{
    public static string FormatHumanReadableUtc(DateTimeOffset utcDateTime, DateTimeOffset now)
    {
        var formattedDateTime = utcDateTime.UtcDateTime.ToString(
            "dddd, MMMM d, yyyy 'at' h:mm tt 'UTC'",
            CultureInfo.InvariantCulture);
        var relativeDateTime = utcDateTime.UtcDateTime.Humanize(dateToCompareAgainst: now.UtcDateTime);

        return $"{formattedDateTime} ({relativeDateTime})";
    }
}
