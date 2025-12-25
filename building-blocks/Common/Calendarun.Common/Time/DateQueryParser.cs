using System.Globalization;

namespace Calendarun.Common.Time;

public static class DateQueryParser
{
    private const string DateOnlyFormat = "yyyy-MM-dd";

    public static bool TryParseDateFilter(
        string? input,
        string paramName,
        bool endExclusive,
        out DateTimeOffset? value,
        out string? error)
    {
        value = null;
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        if (DateTime.TryParseExact(
                input,
                DateOnlyFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dateOnly))
        {
            var utc = new DateTime(
                dateOnly.Year,
                dateOnly.Month,
                dateOnly.Day,
                0,
                0,
                0,
                DateTimeKind.Utc);

            if (endExclusive)
            {
                utc = utc.AddDays(1);
            }

            value = new DateTimeOffset(utc, TimeSpan.Zero);
            return true;
        }

        if (DateTimeOffset.TryParse(
                input,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dateTimeOffset))
        {
            value = dateTimeOffset;
            return true;
        }

        error =
            $"The '{paramName}' parameter must be in {DateOnlyFormat} format or a valid ISO-8601 timestamp. " +
            $"Received: '{input}'.";
        return false;
    }
}
