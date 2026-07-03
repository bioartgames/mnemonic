using Mnemonic;
using Mnemonic.Heuristic;
using Xunit;

namespace Mnemonic.Core.Tests.Heuristic;

public sealed class HeuristicCatalogTests
{
    [Fact]
    public void Entries_contains_expected_types_and_defaults()
    {
        Assert.Equal(28, HeuristicCatalog.Entries.Count);
        AssertEntry("playtest_start", 7, MnemonicConstants.ScoreCapPlaytestStart);
        AssertEntry("playtest_ongoing", 7, 1);
        AssertEntry("runtime_error", 9, MnemonicConstants.ScoreCapRuntimeError);
        AssertEntry("git_commit", 9, MnemonicConstants.ScoreCapGitCommit);
        AssertEntry("git_push", 8, MnemonicConstants.ScoreCapGitPush);
        AssertEntry("debug_session_start", 6, MnemonicConstants.ScoreCapDebugSessionStart);
        AssertEntry("debug_session_stop", 0, 0);
        AssertEntry("script_save", 5, MnemonicConstants.ScoreCapScriptSave);
        AssertEntry("editor_focused_session", 8, MnemonicConstants.ScoreCapEditorFocusedSession);
        AssertEntry("commit_after_playtest", 10, 0);
        AssertEntry("playtest_stop", 0, 0);
    }

    private static void AssertEntry(string type, int weight, int cap)
    {
        var entry = HeuristicCatalog.TryGet(type);
        Assert.NotNull(entry);
        Assert.Equal(weight, entry!.DefaultWeight);
        Assert.Equal(cap, entry.DefaultCap);
    }
}
