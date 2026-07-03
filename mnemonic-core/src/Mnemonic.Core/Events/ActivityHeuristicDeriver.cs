using Mnemonic;

namespace Mnemonic.Events;

public sealed class ActivityHeuristicDeriver
{
    private double _lastEditIntensityUnix = double.NegativeInfinity;

    public IEnumerable<SessionEvent> Process(SessionEvent evt)
    {
        switch (evt.Type)
        {
            case "activity_packet":
                foreach (var derived in ProcessPacket(evt))
                {
                    yield return derived;
                }

                break;
            case "git_commit":
                if (_lastEditIntensityUnix > 0
                    && evt.T - _lastEditIntensityUnix <= MnemonicConstants.CheckpointAfterWorkWindowSeconds)
                {
                    yield return SessionEvent.Create(evt.T, "checkpoint_after_work");
                }

                break;
        }
    }

    private IEnumerable<SessionEvent> ProcessPacket(SessionEvent packet)
    {
        var windowSec = SessionEventExtras.GetInt(packet, "window_sec", 60);
        var sceneSaves = SessionEventExtras.GetInt(packet, "scene_saves");
        var resourceSaves = SessionEventExtras.GetInt(packet, "resource_saves");
        var sceneTransitions = SessionEventExtras.GetInt(packet, "scene_transitions");
        var playtestActiveSec = SessionEventExtras.GetDouble(packet, "playtest_active_sec");
        var focusScriptSec = SessionEventExtras.GetInt(packet, "focus_script_sec");
        var focus2dSec = SessionEventExtras.GetInt(packet, "focus_2d_sec");
        var focus3dSec = SessionEventExtras.GetInt(packet, "focus_3d_sec");
        var actions = sceneSaves + resourceSaves + sceneTransitions;
        var minActions = ActivityPacketThresholds.MinActions(windowSec);
        var minTransitions = ActivityPacketThresholds.MinTransitions(windowSec);
        var focusDominantSec = ActivityPacketThresholds.FocusDominantSec(windowSec);
        var playtestRounded = (int)Math.Round(playtestActiveSec);

        if (actions >= minActions
            && playtestActiveSec < MnemonicConstants.EditIntensityMaxPlaytestSec)
        {
            _lastEditIntensityUnix = packet.T;
            yield return CreateDerived(
                packet.T,
                "edit_intensity",
                $"{sceneSaves} scene, {resourceSaves} resource, {sceneTransitions} transitions, {playtestRounded}s playtest in {windowSec}s");
        }

        if (sceneTransitions >= minTransitions
            && playtestActiveSec <= 0.0)
        {
            yield return CreateDerived(
                packet.T,
                "scene_hopping",
                $"{sceneTransitions} transitions, {playtestRounded}s playtest in {windowSec}s");
        }

        if (focusScriptSec >= focusDominantSec
            && resourceSaves >= MnemonicConstants.ScriptFocusMinResourceSaves
            && playtestActiveSec < MnemonicConstants.EditIntensityMaxPlaytestSec)
        {
            yield return CreateDerived(
                packet.T,
                "script_focus",
                $"{focusScriptSec}s script focus, {resourceSaves} resource saves, {playtestRounded}s playtest in {windowSec}s");
        }

        if ((focus2dSec >= focusDominantSec || focus3dSec >= focusDominantSec)
            && sceneTransitions >= MnemonicConstants.LayoutFocusMinTransitions
            && playtestActiveSec <= 0.0)
        {
            yield return CreateDerived(
                packet.T,
                "layout_focus",
                $"{focus2dSec}s 2D / {focus3dSec}s 3D focus, {sceneTransitions} transitions, {playtestRounded}s playtest in {windowSec}s");
        }
    }

    private static SessionEvent CreateDerived(double t, string type, string detail) =>
        SessionEvent.Create(
            t,
            type,
            new Dictionary<string, object?> { ["pattern_detail"] = detail });
}
