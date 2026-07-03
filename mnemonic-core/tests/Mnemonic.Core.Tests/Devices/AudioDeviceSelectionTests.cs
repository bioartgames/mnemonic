using Mnemonic.Devices;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Xunit;

namespace Mnemonic.Core.Tests.Devices;

public sealed class AudioDeviceSelectionTests
{
    private static readonly IReadOnlyList<AudioDeviceOption> SampleDevices =
    [
        new("", "(System default)"),
        new("{0.0.1.00000000}.{abcdef12-3456-7890-abcd-ef1234567890}", "Microphone (Realtek(R) Audio)"),
        new("{0.0.1.00000000}.{22222222-2222-2222-2222-222222222222}", "Headset Microphone (Realtek(R) Audio)"),
    ];

    private static readonly IReadOnlyList<AudioDeviceOption> SampleRenderDevices =
    [
        new("", "(System default)"),
        new("{0.0.0.00000000}.{33333333-3333-3333-3333-333333333333}", "Speakers (Realtek(R) Audio)"),
    ];

    [Fact]
    public void FindIndexForId_empty_id_selects_system_default()
    {
        Assert.Equal(0, AudioDeviceSelection.FindIndexForId(SampleDevices, ""));
    }

    [Fact]
    public void FindIndexForId_matches_realtek_mic_id_case_insensitively()
    {
        var id = SampleDevices[1].Id;
        var upper = id.ToUpperInvariant();

        Assert.Equal(1, AudioDeviceSelection.FindIndexForId(SampleDevices, id));
        Assert.Equal(1, AudioDeviceSelection.FindIndexForId(SampleDevices, upper));
    }

    [Fact]
    public void FindIndexForId_unknown_id_falls_back_to_system_default()
    {
        Assert.Equal(0, AudioDeviceSelection.FindIndexForId(SampleDevices, "{missing}"));
    }

    [Fact]
    public void RoundTrip_realtek_mic_and_desktop_ids_are_preserved()
    {
        var micId = SampleDevices[1].Id;
        var desktopId = SampleRenderDevices[1].Id;

        var micIndex = AudioDeviceSelection.FindIndexForId(SampleDevices, micId);
        var desktopIndex = AudioDeviceSelection.FindIndexForId(SampleRenderDevices, desktopId);

        Assert.True(AudioDeviceSelection.TryGetDeviceAt(SampleDevices, micIndex, out var mic));
        Assert.True(AudioDeviceSelection.TryGetDeviceAt(SampleRenderDevices, desktopIndex, out var desktop));

        var settings = new AppSettings();
        AudioDeviceSelection.ApplyMicSelection(settings, mic);
        AudioDeviceSelection.ApplyDesktopSelection(settings, desktop);

        Assert.Equal(micId, settings.MicDeviceId);
        Assert.Equal(desktopId, settings.DesktopLoopbackDeviceId);
        Assert.True(settings.CaptureMicEnabled);
        Assert.True(settings.CaptureDesktopEnabled);
    }

    [Fact]
    public void LegacyPopulateCombo_exact_match_only_fails_when_id_differs_by_case()
    {
        var savedId = SampleDevices[1].Id;
        var mismatchedCaseId = savedId.ToUpperInvariant();

        var legacyIndex = FindIndexLegacyOrdinal(SampleDevices, mismatchedCaseId);
        var fixedIndex = AudioDeviceSelection.FindIndexForId(SampleDevices, mismatchedCaseId);

        Assert.Equal(0, legacyIndex);
        Assert.Equal(1, fixedIndex);
    }

    [Fact]
    public void Legacy_baseline_from_ui_wipes_saved_mic_when_restore_fails_and_user_saves_other_field()
    {
        var savedMicId = SampleDevices[1].Id;
        var settings = new AppSettings
        {
            MicDeviceId = savedMicId,
            CaptureMicEnabled = true,
            SegmentDurationSeconds = 120,
        };

        var comboIndex = FindIndexLegacyOrdinal(SampleDevices, "{missing-endpoint}");
        Assert.Equal(0, comboIndex);

        var legacyBaseline = AppSettingsCaptureBinding.Snapshot(ReadCaptureFieldsFromComboAlways(
            settings,
            SampleDevices,
            comboIndex,
            SampleRenderDevices,
            desktopIndex: 0));
        var fromUi = ReadCaptureFieldsFromComboAlways(
            settings,
            SampleDevices,
            comboIndex,
            SampleRenderDevices,
            desktopIndex: 0);
        fromUi.SegmentDurationSeconds = 150;

        Assert.False(AppSettingsCaptureBinding.CaptureFieldsEqual(legacyBaseline, fromUi));

        AppSettingsCaptureBinding.ApplyCaptureFields(settings, fromUi);
        Assert.Equal("", settings.MicDeviceId);
    }

    [Fact]
    public void Unchanged_combo_index_preserves_saved_mic_when_restore_shows_system_default()
    {
        var savedMicId = SampleDevices[1].Id;
        var settings = new AppSettings
        {
            MicDeviceId = savedMicId,
            CaptureMicEnabled = true,
            SegmentDurationSeconds = 120,
        };

        var comboIndex = FindIndexLegacyOrdinal(SampleDevices, "{missing-endpoint}");
        var micIndexAtLoad = comboIndex;
        var fromUi = ReadCaptureFieldsFromCombo(
            settings,
            SampleDevices,
            comboIndex,
            SampleRenderDevices,
            desktopIndex: 0,
            micIndexAtLoad,
            desktopIndexAtLoad: 0);
        fromUi.SegmentDurationSeconds = 150;

        AppSettingsCaptureBinding.ApplyCaptureFields(settings, fromUi);

        Assert.Equal(savedMicId, settings.MicDeviceId);
        Assert.Equal(150, settings.SegmentDurationSeconds);
    }

    [Fact]
    public void SelectedIndex_read_preserves_realtek_device_after_case_mismatch_restore()
    {
        var savedMicId = SampleDevices[1].Id;
        var settings = new AppSettings { MicDeviceId = savedMicId, CaptureMicEnabled = true };

        var comboIndex = AudioDeviceSelection.FindIndexForId(SampleDevices, settings.MicDeviceId.ToUpperInvariant());
        Assert.True(AudioDeviceSelection.TryGetDeviceAt(SampleDevices, comboIndex, out var mic));

        var fromUi = AppSettingsCaptureBinding.Snapshot(settings);
        AudioDeviceSelection.ApplyMicSelection(fromUi, mic);

        Assert.Equal(savedMicId, fromUi.MicDeviceId);
        Assert.True(fromUi.CaptureMicEnabled);
    }

    private static int FindIndexLegacyOrdinal(IReadOnlyList<AudioDeviceOption> devices, string selectedId)
    {
        var selectedIndex = 0;
        for (var i = 0; i < devices.Count; i++)
        {
            if (devices[i].Id == selectedId)
            {
                selectedIndex = i;
            }
        }

        return selectedIndex;
    }

    private static AppSettings ReadCaptureFieldsFromComboAlways(
        AppSettings seed,
        IReadOnlyList<AudioDeviceOption> micDevices,
        int micIndex,
        IReadOnlyList<AudioDeviceOption> desktopDevices,
        int desktopIndex) =>
        ReadCaptureFieldsFromCombo(
            seed,
            micDevices,
            micIndex,
            desktopDevices,
            desktopIndex,
            micIndexAtLoad: int.MinValue + 1,
            desktopIndexAtLoad: int.MinValue + 1);

    private static AppSettings ReadCaptureFieldsFromCombo(
        AppSettings seed,
        IReadOnlyList<AudioDeviceOption> micDevices,
        int micIndex,
        IReadOnlyList<AudioDeviceOption> desktopDevices,
        int desktopIndex,
        int micIndexAtLoad = int.MinValue,
        int desktopIndexAtLoad = int.MinValue)
    {
        var result = AppSettingsCaptureBinding.Snapshot(seed);
        if (micIndexAtLoad == int.MinValue)
        {
            micIndexAtLoad = micIndex;
        }

        if (desktopIndexAtLoad == int.MinValue)
        {
            desktopIndexAtLoad = desktopIndex;
        }

        if (micIndex != micIndexAtLoad)
        {
            if (AudioDeviceSelection.TryGetDeviceAt(micDevices, micIndex, out var mic))
            {
                AudioDeviceSelection.ApplyMicSelection(result, mic);
            }
            else
            {
                AudioDeviceSelection.ClearMicSelection(result);
            }
        }

        if (desktopIndex != desktopIndexAtLoad)
        {
            if (AudioDeviceSelection.TryGetDeviceAt(desktopDevices, desktopIndex, out var desktop))
            {
                AudioDeviceSelection.ApplyDesktopSelection(result, desktop);
            }
            else
            {
                AudioDeviceSelection.ClearDesktopSelection(result);
            }
        }

        return result;
    }
}
