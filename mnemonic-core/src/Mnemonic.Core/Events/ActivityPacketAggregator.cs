using Mnemonic;

namespace Mnemonic.Events;

public sealed class ActivityPacketAggregator
{
    private int _windowSeconds = (int)MnemonicConstants.ActivityPacketReferenceWindowSeconds;
    private double _windowStartUnix = double.NegativeInfinity;
    private int _sceneSaves;
    private int _resourceSaves;
    private int _sceneTransitions;
    private readonly HashSet<string> _distinctPaths = new(StringComparer.Ordinal);
    private double _playtestActiveSec;
    private double? _playtestOpenUnix;
    private string _focusBucket = "other";
    private double _focusBucketSinceUnix = double.NegativeInfinity;
    private double _focusScriptSec;
    private double _focus2dSec;
    private double _focus3dSec;
    private double _focusInspectorSec;
    private double _focusOtherSec;

    public void SetWindowSeconds(int windowSeconds)
    {
        _windowSeconds = Math.Clamp(
            windowSeconds,
            ActivityPacketWindowPolicy.MinSeconds,
            ActivityPacketWindowPolicy.MaxSeconds);
    }

    public IEnumerable<SessionEvent> OnRaw(SessionEvent raw)
    {
        var emitted = new List<SessionEvent>();
        if (_windowStartUnix < 0)
        {
            _windowStartUnix = raw.T;
            _focusBucketSinceUnix = raw.T;
        }

        ApplyFocusThrough(raw.T);
        IngestRaw(raw);

        while (raw.T - _windowStartUnix >= _windowSeconds)
        {
            var packetEnd = _windowStartUnix + _windowSeconds;
            emitted.Add(BuildPacket(packetEnd));
            AdvanceWindow(packetEnd);
        }

        return emitted;
    }

    public IEnumerable<SessionEvent> Flush(double flushUnix)
    {
        if (_windowStartUnix < 0)
        {
            return [];
        }

        ApplyFocusThrough(flushUnix);
        if (!HasActivity())
        {
            return [];
        }

        var packetEnd = flushUnix - 0.001;
        if (packetEnd <= _windowStartUnix)
        {
            packetEnd = flushUnix;
        }

        var packet = BuildPacket(packetEnd);
        AdvanceWindow(flushUnix);
        return [packet];
    }

    private void IngestRaw(SessionEvent raw)
    {
        switch (raw.Type)
        {
            case "scene_save":
                _sceneSaves++;
                AddPath(SessionEventExtras.GetString(raw, "path"));
                break;
            case "resource_saved":
                _resourceSaves++;
                AddPath(SessionEventExtras.GetString(raw, "path"));
                break;
            case "scene_transition":
                _sceneTransitions++;
                AddPath(SessionEventExtras.GetString(raw, "to_scene"));
                break;
            case "playtest_start":
                if (_playtestOpenUnix is null)
                {
                    _playtestOpenUnix = raw.T;
                }

                break;
            case "playtest_stop":
                if (_playtestOpenUnix is not null)
                {
                    _playtestActiveSec += Math.Max(0, raw.T - _playtestOpenUnix.Value);
                    _playtestOpenUnix = null;
                }

                break;
            case "editor_focus_changed":
                var bucket = SessionEventExtras.GetString(raw, "focus") ?? "other";
                _focusBucket = NormalizeFocusBucket(bucket);
                _focusBucketSinceUnix = raw.T;
                break;
        }
    }

    private SessionEvent BuildPacket(double packetEndUnix)
    {
        ApplyFocusThrough(packetEndUnix);
        if (_playtestOpenUnix is not null)
        {
            _playtestActiveSec += Math.Max(0, packetEndUnix - _playtestOpenUnix.Value);
            _playtestOpenUnix = packetEndUnix;
        }

        var paths = _distinctPaths.OrderBy(p => p, StringComparer.Ordinal)
            .Take(MnemonicConstants.ActivityPacketMaxDistinctPaths)
            .ToArray();

        return SessionEvent.Create(
            packetEndUnix,
            "activity_packet",
            new Dictionary<string, object?>
            {
                ["window_sec"] = _windowSeconds,
                ["scene_saves"] = _sceneSaves,
                ["resource_saves"] = _resourceSaves,
                ["scene_transitions"] = _sceneTransitions,
                ["distinct_paths"] = paths,
                ["playtest_active_sec"] = (int)Math.Round(_playtestActiveSec),
                ["focus_script_sec"] = (int)Math.Round(_focusScriptSec),
                ["focus_2d_sec"] = (int)Math.Round(_focus2dSec),
                ["focus_3d_sec"] = (int)Math.Round(_focus3dSec),
                ["focus_inspector_sec"] = (int)Math.Round(_focusInspectorSec),
                ["focus_other_sec"] = (int)Math.Round(_focusOtherSec),
            });
    }

    private void AdvanceWindow(double packetEndUnix)
    {
        _windowStartUnix = packetEndUnix;
        _sceneSaves = 0;
        _resourceSaves = 0;
        _sceneTransitions = 0;
        _distinctPaths.Clear();
        _playtestActiveSec = 0;
        _focusScriptSec = 0;
        _focus2dSec = 0;
        _focus3dSec = 0;
        _focusInspectorSec = 0;
        _focusOtherSec = 0;
        _focusBucketSinceUnix = packetEndUnix;
    }

    private bool HasActivity() =>
        _sceneSaves + _resourceSaves + _sceneTransitions > 0
        || _playtestOpenUnix is not null
        || _playtestActiveSec > 0
        || _focusScriptSec + _focus2dSec + _focus3dSec + _focusInspectorSec + _focusOtherSec > 0;

    private void ApplyFocusThrough(double untilUnix)
    {
        if (_focusBucketSinceUnix < 0 || untilUnix <= _focusBucketSinceUnix)
        {
            return;
        }

        var delta = untilUnix - _focusBucketSinceUnix;
        switch (_focusBucket)
        {
            case "script":
                _focusScriptSec += delta;
                break;
            case "2d":
                _focus2dSec += delta;
                break;
            case "3d":
                _focus3dSec += delta;
                break;
            case "inspector":
                _focusInspectorSec += delta;
                break;
            default:
                _focusOtherSec += delta;
                break;
        }

        _focusBucketSinceUnix = untilUnix;
    }

    private void AddPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _distinctPaths.Add(path);
    }

    private static string NormalizeFocusBucket(string bucket) =>
        bucket switch
        {
            "script" => "script",
            "2d" => "2d",
            "3d" => "3d",
            "inspector" => "inspector",
            _ => "other",
        };
}
