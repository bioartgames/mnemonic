using Mnemonic.Events;
using Xunit;

namespace Mnemonic.Core.Tests.Events;

public sealed class SessionEventStoreTests
{
    [Fact]
    public void EventsBetween_includes_lower_bound_excludes_upper()
    {
        var store = new SessionEventStore();
        store.Append(SessionEvent.Create(100, "scene_save"));
        store.Append(SessionEvent.Create(150, "git_commit"));
        store.Append(SessionEvent.Create(200, "playtest_start"));

        var window = store.EventsBetween(100, 200);

        Assert.Equal(2, window.Count);
        Assert.Equal("scene_save", window[0].Type);
        Assert.Equal("git_commit", window[1].Type);
    }

    [Fact]
    public void Trim_drops_oldest_when_over_max()
    {
        var store = new SessionEventStore();
        for (var i = 0; i < MnemonicConstants.SessionEventsMaxEntries + 1; i++)
        {
            store.Append(SessionEvent.Create(i, "scene_save"));
        }

        Assert.Equal(MnemonicConstants.SessionEventsMaxEntries, store.Count);
        var window = store.EventsBetween(0, MnemonicConstants.SessionEventsMaxEntries + 10.0);
        Assert.DoesNotContain(window, e => e.T == 0);
        Assert.Contains(window, e => e.T == 1);
    }
}
