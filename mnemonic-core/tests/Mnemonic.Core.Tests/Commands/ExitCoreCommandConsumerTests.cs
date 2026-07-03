using Mnemonic.Commands;
using Mnemonic.Ipc;
using Xunit;

namespace Mnemonic.Core.Tests.Commands;

public sealed class ExitCoreCommandConsumerTests
{
    [Fact]
    public void TryConsume_exit_deletes_file_and_invokes_shutdown()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            Directory.CreateDirectory(paths.CommandsDir);
            File.WriteAllText(paths.ExitCoreFile, "{}");

            var shutdownCount = 0;
            var consumer = new ExitCoreCommandConsumer(paths, () => shutdownCount++);

            Assert.True(consumer.TryConsume());
            Assert.Equal(1, shutdownCount);
            Assert.False(File.Exists(paths.ExitCoreFile));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryConsume_shutdown_handler_exception_deletes_file()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            Directory.CreateDirectory(paths.CommandsDir);
            File.WriteAllText(paths.ExitCoreFile, "{}");

            var shutdownCount = 0;
            var consumer = new ExitCoreCommandConsumer(
                paths,
                () =>
                {
                    shutdownCount++;
                    throw new InvalidOperationException("shutdown failed");
                });

            Assert.False(consumer.TryConsume());
            Assert.Equal(1, shutdownCount);
            Assert.False(File.Exists(paths.ExitCoreFile));
            Assert.False(consumer.TryConsume());
            Assert.Equal(1, shutdownCount);
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
            File.WriteAllText(paths.ExitCoreFile, "not-json");

            var shutdownCount = 0;
            var consumer = new ExitCoreCommandConsumer(paths, () => shutdownCount++);

            Assert.False(consumer.TryConsume());
            Assert.Equal(0, shutdownCount);
            Assert.False(File.Exists(paths.ExitCoreFile));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempRoot()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"mnemonic_exit_core_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
