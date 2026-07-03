using Mnemonic.Capture;
using Xunit;

namespace Mnemonic.Core.Tests.Capture;

public sealed class FfmpegCaptureExitSummaryTests
{
    [Fact]
    public void BuildUnexpectedExitMessage_without_log_returns_code_only()
    {
        var message = FfmpegCaptureExitSummary.BuildUnexpectedExitMessage(1, null);
        Assert.Equal("FFmpeg capture exited (code 1)", message);
    }

    [Fact]
    public void BuildUnexpectedExitMessage_with_missing_log_returns_code_only()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mnemonic-missing-log-{Guid.NewGuid():N}.log");
        var message = FfmpegCaptureExitSummary.BuildUnexpectedExitMessage(-1, path);
        Assert.Equal("FFmpeg capture exited (code -1)", message);
    }

    [Fact]
    public void BuildUnexpectedExitMessage_appends_log_tail()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "line1\nerror: device busy\n");
            var message = FfmpegCaptureExitSummary.BuildUnexpectedExitMessage(2, path);
            Assert.StartsWith("FFmpeg capture exited (code 2): ", message);
            Assert.Contains("device busy", message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void BuildUnexpectedExitMessage_truncates_long_log_tail()
    {
        var path = Path.GetTempFileName();
        try
        {
            var longLine = new string('x', MnemonicConstants.FfmpegCaptureExitStatusTailChars + 50);
            File.WriteAllText(path, longLine);
            var message = FfmpegCaptureExitSummary.BuildUnexpectedExitMessage(0, path);
            var tail = message["FFmpeg capture exited (code 0): ".Length..];
            Assert.Equal(MnemonicConstants.FfmpegCaptureExitStatusTailChars, tail.Length);
            Assert.Equal('x', tail[0]);
            Assert.Equal('x', tail[^1]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
