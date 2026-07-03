using System.Globalization;

namespace Mnemonic.Display;

public static class DisplayTimeFormat
{
    public static string FormatLocalDateTime(long unixSeconds)
    {
        var dt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime();
        return dt.ToString("MMM d, yyyy, h:mm tt", CultureInfo.InvariantCulture);
    }

    public static string FormatLocalTimeRange(long openUnix, long closeUnix)
    {
        var open = DateTimeOffset.FromUnixTimeSeconds(openUnix).ToLocalTime();
        var close = DateTimeOffset.FromUnixTimeSeconds(closeUnix).ToLocalTime();
        var datePart = open.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
        return $"{datePart}, {open:h:mm tt}–{close:h:mm tt}";
    }
}
