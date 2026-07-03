using Mnemonic.Ipc.Models;
using Mnemonic.Windows.Tray;
using Xunit;

namespace Mnemonic.Windows.Tests.Tray;

public sealed class SettingsFormApplyTests
{
    [Fact]
    public void ApplyTrayFieldsFromUi_copies_retention_fields_from_ui_snapshot()
    {
        var target = new AppSettings
        {
            HighlightScoreMin = 20,
            SegmentHistoryMaxEntries = 200,
            DrawMouse = true,
            SegmentDurationSeconds = 120,
            PreserveThreshold = 10,
            NotableScoreMin = 10,
        };
        var fromUi = new AppSettings
        {
            HighlightScoreMin = 25,
            SegmentHistoryMaxEntries = 500,
            DrawMouse = false,
            SegmentDurationSeconds = 180,
            PreserveThreshold = 15,
            NotableScoreMin = 12,
        };

        SettingsFormApply.ApplyTrayFieldsFromUi(target, fromUi);

        Assert.Equal(25, target.HighlightScoreMin);
        Assert.Equal(500, target.SegmentHistoryMaxEntries);
        Assert.False(target.DrawMouse);
        Assert.Equal(180, target.SegmentDurationSeconds);
        Assert.Equal(15, target.PreserveThreshold);
        Assert.Equal(12, target.NotableScoreMin);
    }
}
