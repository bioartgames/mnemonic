using Mnemonic;

namespace Mnemonic.Events;

public static class SegmentCloseActivityEvaluator
{
    public static void AppendSynthetic(IList<SessionEvent> events, double tOpenUnix, double tCloseUnix)
    {
        if (tCloseUnix - tOpenUnix < MnemonicConstants.LongEditSpanMinSegmentSeconds)
        {
            return;
        }

        if (events.Any(e => e.Type == "playtest_start"))
        {
            return;
        }

        var editorEvents = events.Count(e => e.Type is "scene_save" or "resource_saved" or "scene_transition");
        if (editorEvents < MnemonicConstants.LongEditSpanMinEditorEvents)
        {
            return;
        }

        events.Add(SessionEvent.Create(tCloseUnix, "long_edit_span"));
    }
}
