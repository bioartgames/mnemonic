using Mnemonic.Display;
using Xunit;

namespace Mnemonic.Core.Tests.Display;

public sealed class DisplayTimeFormatTests
{
    private static long LocalUnix(int year, int month, int day, int hour, int minute, int second = 0)
    {
        var local = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
        return new DateTimeOffset(local).ToUnixTimeSeconds();
    }

    [Fact]
    public void FormatLocalDateTime_uses_twelve_hour_without_leading_hour_zero()
    {
        var text = DisplayTimeFormat.FormatLocalDateTime(LocalUnix(2026, 6, 6, 7, 58));
        Assert.Contains("7:58 AM", text);
        Assert.DoesNotContain("07:58", text);
    }

    [Fact]
    public void FormatLocalTimeRange_uses_en_dash_between_times()
    {
        var text = DisplayTimeFormat.FormatLocalTimeRange(
            LocalUnix(2026, 6, 6, 7, 58),
            LocalUnix(2026, 6, 6, 8, 0));
        Assert.Contains("7:58 AM", text);
        Assert.Contains("8:00 AM", text);
        Assert.Contains("–", text);
    }

    [Fact]
    public void FormatLocalDateTime_handles_noon_and_midnight()
    {
        Assert.Contains("12:00 PM", DisplayTimeFormat.FormatLocalDateTime(LocalUnix(2026, 6, 6, 12, 0)));
        Assert.Contains("12:05 AM", DisplayTimeFormat.FormatLocalDateTime(LocalUnix(2026, 6, 6, 0, 5)));
    }
}
