using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Xunit;

namespace Mnemonic.Core.Tests.Ipc;

public sealed class StatusStorePendingManualPreserveTests
{
    [Fact]
    public void SetPendingManualPreserve_writes_segment_index()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new StatusStore(paths);
            store.SetPendingManualPreserve(7);

            var snapshot = store.Read();
            Assert.NotNull(snapshot);
            Assert.Equal(7, snapshot!.PendingManualPreserveSegmentIndex);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ClearPendingManualPreserve_clears_matching_segment()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new StatusStore(paths);
            store.SetPendingManualPreserve(3);
            store.ClearPendingManualPreserve(3);

            var snapshot = store.Read();
            Assert.NotNull(snapshot);
            Assert.Equal(-1, snapshot!.PendingManualPreserveSegmentIndex);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Write_preserves_pending_manual_preserve_across_capture_status_writes()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new DataRootPaths(root);
            var store = new StatusStore(paths);
            store.SetPendingManualPreserve(5);

            store.Write(new StatusSnapshot
            {
                ContractVersion = MnemonicConstants.IpcContractVersion,
                Recording = true,
                State = "recording",
                CurrentSegmentIndex = 5,
            });

            var snapshot = store.Read();
            Assert.NotNull(snapshot);
            Assert.Equal(5, snapshot!.PendingManualPreserveSegmentIndex);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "mnemonic_pending_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
