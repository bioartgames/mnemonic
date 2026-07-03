using Mnemonic.Commands;
using Xunit;

namespace Mnemonic.Core.Tests.Commands;

public sealed class CommandFileHelperTests
{
    [Fact]
    public void TryConsume_not_present_returns_NotPresent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mnemonic_cmd_helper_{Guid.NewGuid():N}.json");
        var result = CommandFileHelper.TryConsume<RebuildClipsIndexCommand>(
            path,
            "test_command",
            () => { });
        Assert.Equal(CommandConsumeResult.NotPresent, result);
    }

    [Fact]
    public void TryConsume_invalid_json_deletes_and_returns_InvalidJson()
    {
        var path = WriteTempCommand("not-json");
        try
        {
            var result = CommandFileHelper.TryConsume<RebuildClipsIndexCommand>(
                path,
                "test_command",
                () => { });
            Assert.Equal(CommandConsumeResult.InvalidJson, result);
            Assert.False(File.Exists(path));
        }
        finally
        {
            TryDeleteFile(path);
        }
    }

    [Fact]
    public void TryConsume_handler_failure_deletes_and_returns_HandlerFailed()
    {
        var path = WriteTempCommand("{}");
        try
        {
            var result = CommandFileHelper.TryConsume<RebuildClipsIndexCommand>(
                path,
                "test_command",
                () => throw new InvalidOperationException("boom"));
            Assert.Equal(CommandConsumeResult.HandlerFailed, result);
            Assert.False(File.Exists(path));
        }
        finally
        {
            TryDeleteFile(path);
        }
    }

    [Fact]
    public void TryConsume_success_deletes_and_returns_Success()
    {
        var path = WriteTempCommand("{}");
        try
        {
            var invoked = false;
            var result = CommandFileHelper.TryConsume<RebuildClipsIndexCommand>(
                path,
                "test_command",
                () => invoked = true);
            Assert.Equal(CommandConsumeResult.Success, result);
            Assert.True(invoked);
            Assert.False(File.Exists(path));
        }
        finally
        {
            TryDeleteFile(path);
        }
    }

    [Fact]
    public void TryConsume_io_error_keeps_file()
    {
        var path = WriteTempCommand("{}");
        try
        {
            using var lockStream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);

            var result = CommandFileHelper.TryConsume<RebuildClipsIndexCommand>(
                path,
                "test_command",
                () => { });
            Assert.Equal(CommandConsumeResult.IoError, result);
            Assert.True(File.Exists(path));
        }
        finally
        {
            TryDeleteFile(path);
        }
    }

    private static string WriteTempCommand(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"mnemonic_cmd_helper_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, contents);
        return path;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private static void TryDeleteDir(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
        }
    }
}
