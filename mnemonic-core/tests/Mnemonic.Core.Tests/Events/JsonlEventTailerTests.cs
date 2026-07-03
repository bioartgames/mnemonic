using Mnemonic.Events;
using Xunit;

namespace Mnemonic.Core.Tests.Events;

public sealed class JsonlEventTailerTests
{
    [Fact]
    public void Poll_reads_incremental_lines()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mnemonic_tail_{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllText(path, "{\"t\":1,\"type\":\"scene_save\"}\n");

            var tailer = new JsonlEventTailer();
            var first = tailer.Poll(path);
            Assert.Single(first);
            Assert.Equal("scene_save", first[0].Type);

            File.AppendAllText(path, "{\"t\":2,\"type\":\"git_commit\"}\n");
            var second = tailer.Poll(path);
            Assert.Single(second);
            Assert.Equal("git_commit", second[0].Type);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Poll_skips_invalid_lines()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mnemonic_tail_{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllText(
                path,
                "not json\n{\"t\":1,\"type\":\"scene_save\"}\n{\"no_type\":true}\n");

            var tailer = new JsonlEventTailer();
            var batch = tailer.Poll(path);
            Assert.Single(batch);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Poll_resets_offset_when_file_truncated()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mnemonic_tail_{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllText(
                path,
                "{\"t\":1,\"type\":\"scene_save\",\"path\":\"padding/for/truncate/test\"}\n");
            var tailer = new JsonlEventTailer();
            tailer.Poll(path);

            File.WriteAllText(path, "{\"t\":9,\"type\":\"git_commit\"}\n");
            var batch = tailer.Poll(path);
            Assert.Single(batch);
            Assert.Equal("git_commit", batch[0].Type);
            Assert.Equal(9, batch[0].T);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
