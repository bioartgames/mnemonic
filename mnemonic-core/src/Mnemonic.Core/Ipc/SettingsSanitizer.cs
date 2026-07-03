using Mnemonic.Capture;
using Mnemonic.Devices;
using Mnemonic.Heuristic;
using Mnemonic.Ipc.Models;
using Mnemonic.Retention;

namespace Mnemonic.Ipc;

public static class SettingsSanitizer
{
    public static bool Sanitize(AppSettings settings)
    {
        var changed = false;
        var captureIds = new HashSet<string>(
            WasapiDeviceLister.ListCaptureDevices().Select(d => d.Id),
            StringComparer.OrdinalIgnoreCase);
        var renderIds = new HashSet<string>(
            WasapiDeviceLister.ListRenderDevices().Select(d => d.Id),
            StringComparer.OrdinalIgnoreCase);

        if (settings.MicDeviceId.Length > 0 && !captureIds.Contains(settings.MicDeviceId))
        {
            settings.MicDeviceId = "";
            settings.CaptureMicEnabled = true;
            changed = true;
        }

        if (settings.DesktopLoopbackDeviceId.Length > 0 && !renderIds.Contains(settings.DesktopLoopbackDeviceId))
        {
            settings.DesktopLoopbackDeviceId = "";
            settings.CaptureDesktopEnabled = true;
            changed = true;
        }

        var normalizedSegment = SegmentDurationPolicy.Normalize(settings.SegmentDurationSeconds);
        if (normalizedSegment != settings.SegmentDurationSeconds)
        {
            settings.SegmentDurationSeconds = normalizedSegment;
            changed = true;
        }

        changed |= ScoreTierNormalizer.ApplyTo(settings);

        var normalizedHistoryMax = SegmentHistoryMaxEntriesPolicy.Clamp(settings.SegmentHistoryMaxEntries);
        if (normalizedHistoryMax != settings.SegmentHistoryMaxEntries)
        {
            settings.SegmentHistoryMaxEntries = normalizedHistoryMax;
            changed = true;
        }

        if (settings.Heuristics is not null)
        {
            var sanitized = new Dictionary<string, HeuristicTypeSettings>(StringComparer.Ordinal);
            foreach (var pair in settings.Heuristics)
            {
                if (HeuristicCatalog.TryGet(pair.Key) is null || pair.Value is null)
                {
                    changed = true;
                    continue;
                }

                var typeSettings = pair.Value;
                typeSettings.Weight = Math.Clamp(typeSettings.Weight, 0, HeuristicSettingsResolver.MaxWeight);
                typeSettings.Cap = Math.Clamp(typeSettings.Cap, 0, HeuristicSettingsResolver.MaxCap);
                sanitized[pair.Key] = typeSettings;
            }

            if (sanitized.Count != settings.Heuristics.Count)
            {
                changed = true;
            }

            settings.Heuristics = sanitized;
        }

        return changed;
    }
}
