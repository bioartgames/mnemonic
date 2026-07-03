using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Xunit;

namespace Mnemonic.Core.Tests.Ipc;

public sealed class StatusStoreLiveClipPreviewTests
{
    [Fact]
    public void Write_preserves_existing_live_preview_by_default()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new StatusStore(paths);
            store.Write(new StatusSnapshot
            {
                ContractVersion = MnemonicConstants.IpcContractVersion,
                Recording = true,
                State = "recording",
                CurrentSegmentIndex = 4,
                LiveClipPreview = new LiveClipPreview { SegmentIndex = 4, SegmentId = "segment_00004" },
            });

            store.Write(new StatusSnapshot
            {
                ContractVersion = MnemonicConstants.IpcContractVersion,
                Recording = false,
                State = "paused",
                CurrentSegmentIndex = 0,
            });

            var snapshot = store.Read();
            Assert.NotNull(snapshot);
            Assert.NotNull(snapshot!.LiveClipPreview);
            Assert.Equal(4, snapshot.LiveClipPreview!.SegmentIndex);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void WriteLiveClipPreview_can_clear_preview()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new StatusStore(paths);
            store.Write(new StatusSnapshot
            {
                ContractVersion = MnemonicConstants.IpcContractVersion,
                Recording = true,
                State = "recording",
                CurrentSegmentIndex = 8,
                LiveClipPreview = new LiveClipPreview { SegmentIndex = 8, SegmentId = "segment_00008" },
            });

            store.WriteLiveClipPreview(null);

            var snapshot = store.Read();
            Assert.NotNull(snapshot);
            Assert.Null(snapshot!.LiveClipPreview);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void WriteLiveClipPreview_round_trips_capture_prefix()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new StatusStore(paths);
            store.WriteLiveClipPreview(new LiveClipPreview
            {
                SegmentIndex = 3,
                SegmentId = "segment_00003",
                CapturePrefix = "mn_1234567890_ab12",
            });

            var snapshot = store.Read();
            Assert.NotNull(snapshot);
            Assert.Equal("mn_1234567890_ab12", snapshot!.LiveClipPreview!.CapturePrefix);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void WriteLiveClipPreview_handles_many_sequential_updates()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new StatusStore(paths);

            for (var i = 0; i < 200; i++)
            {
                store.WriteLiveClipPreview(new LiveClipPreview
                {
                    SegmentIndex = i,
                    SegmentId = $"segment_{i:D5}",
                });
            }

            var snapshot = store.Read();
            Assert.NotNull(snapshot);
            Assert.NotNull(snapshot!.LiveClipPreview);
            Assert.Equal(199, snapshot.LiveClipPreview!.SegmentIndex);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mnemonic_status_live_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "control"));
        return root;
    }
}
