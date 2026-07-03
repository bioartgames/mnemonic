using Mnemonic.Commands;
using Mnemonic.Ipc;
using Xunit;

namespace Mnemonic.Core.Tests.Commands;

public sealed class PauseResumeCommandConsumerTests
{
    [Fact]
    public void TryConsume_pause_deletes_file_and_invokes_pause()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            Directory.CreateDirectory(paths.CommandsDir);
            File.WriteAllText(paths.PauseCaptureFile, "{}");

            var pauseCount = 0;
            var resumeCount = 0;
            var consumer = new PauseResumeCommandConsumer(
                paths,
                () => pauseCount++,
                () => resumeCount++);

            Assert.True(consumer.TryConsume());
            Assert.Equal(1, pauseCount);
            Assert.Equal(0, resumeCount);
            Assert.False(File.Exists(paths.PauseCaptureFile));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryConsume_resume_deletes_file_and_invokes_resume()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            Directory.CreateDirectory(paths.CommandsDir);
            File.WriteAllText(paths.ResumeCaptureFile, "{}");

            var pauseCount = 0;
            var resumeCount = 0;
            var consumer = new PauseResumeCommandConsumer(
                paths,
                () => pauseCount++,
                () => resumeCount++);

            Assert.True(consumer.TryConsume());
            Assert.Equal(0, pauseCount);
            Assert.Equal(1, resumeCount);
            Assert.False(File.Exists(paths.ResumeCaptureFile));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryConsume_pause_handler_exception_deletes_file()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            Directory.CreateDirectory(paths.CommandsDir);
            File.WriteAllText(paths.PauseCaptureFile, "{}");

            var pauseCount = 0;
            var consumer = new PauseResumeCommandConsumer(
                paths,
                () =>
                {
                    pauseCount++;
                    throw new InvalidOperationException("pause failed");
                },
                () => { });

            Assert.False(consumer.TryConsume());
            Assert.Equal(1, pauseCount);
            Assert.False(File.Exists(paths.PauseCaptureFile));
            Assert.False(consumer.TryConsume());
            Assert.Equal(1, pauseCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryConsume_invalid_json_deletes_file_without_invoke()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            Directory.CreateDirectory(paths.CommandsDir);
            File.WriteAllText(paths.PauseCaptureFile, "not-json");

            var pauseCount = 0;
            var consumer = new PauseResumeCommandConsumer(
                paths,
                () => pauseCount++,
                () => { });

            Assert.False(consumer.TryConsume());
            Assert.Equal(0, pauseCount);
            Assert.False(File.Exists(paths.PauseCaptureFile));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempRoot()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mnemonic_pause_resume_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
