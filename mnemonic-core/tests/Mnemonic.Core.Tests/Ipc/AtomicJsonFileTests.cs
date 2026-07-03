using System.Text.Json;
using Mnemonic.Ipc;
using Xunit;

namespace Mnemonic.Core.Tests.Ipc;

public sealed class AtomicJsonFileTests
{
    [Fact]
    public async Task Write_retries_and_succeeds_after_transient_target_lock()
    {
        Assert.True(OperatingSystem.IsWindows(), "Atomic file lock semantics test requires Windows.");

        var root = CreateTempRoot();
        try
        {
            var path = Path.Combine(root, "control", "status.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            AtomicJsonFile.Write(path, new SamplePayload { Value = "before" }, JsonOptions.Shared);

            var lockHandle = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var releaseTask = Task.Run(async () =>
            {
                await Task.Delay(120);
                lockHandle.Dispose();
            });

            AtomicJsonFile.Write(path, new SamplePayload { Value = "after" }, JsonOptions.Shared);
            await releaseTask;

            var loaded = AtomicJsonFile.Read<SamplePayload>(path, JsonOptions.Shared);
            Assert.NotNull(loaded);
            Assert.Equal("after", loaded!.Value);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Write_throws_when_target_lock_persists_past_retry_budget()
    {
        Assert.True(OperatingSystem.IsWindows(), "Atomic file lock semantics test requires Windows.");

        var root = CreateTempRoot();
        try
        {
            var path = Path.Combine(root, "control", "status.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            AtomicJsonFile.Write(path, new SamplePayload { Value = "before" }, JsonOptions.Shared);

            using var lockHandle = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            var ex = Record.Exception(() =>
                AtomicJsonFile.Write(path, new SamplePayload { Value = "after" }, JsonOptions.Shared));
            Assert.NotNull(ex);
            Assert.True(ex is UnauthorizedAccessException or IOException);

            var loaded = AtomicJsonFile.Read<SamplePayload>(path, JsonOptions.Shared);
            Assert.NotNull(loaded);
            Assert.Equal("before", loaded!.Value);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mnemonic_atomic_json_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class SamplePayload
    {
        public string Value { get; init; } = "";
    }
}
