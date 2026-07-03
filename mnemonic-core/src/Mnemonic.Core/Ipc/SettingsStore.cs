using System.Text.Json;
using Mnemonic.Ipc.Models;

namespace Mnemonic.Ipc;

public sealed class SettingsStore
{
    private readonly DataRootPaths _paths;
    private DateTime _settingsFileWriteUtc = DateTime.MinValue;

    public SettingsStore(DataRootPaths paths)
    {
        _paths = paths;
    }

    public AppSettings Load()
    {
        if (!File.Exists(_paths.SettingsFile))
        {
            var defaults = CreateInitialDefaults();
            Save(defaults);
            return defaults;
        }

        try
        {
            var loaded = AtomicJsonFile.Read<AppSettings>(_paths.SettingsFile, SettingsJsonOptions.Shared);
            if (loaded is null)
            {
                var defaults = CreateInitialDefaults();
                Save(defaults);
                return defaults;
            }

            RefreshWriteTimestamp();
            MigrateLegacySilentAudioIfNeeded(loaded);
            return loaded;
        }
        catch (JsonException)
        {
            var defaults = CreateInitialDefaults();
            Save(defaults);
            return defaults;
        }
    }

    /// <summary>
    /// Applies Hook-owned fields from disk into <paramref name="target"/> when settings.json changed.
    /// Mutates <paramref name="target"/> in place so all Core services sharing the instance stay in sync.
    /// </summary>
    public void TryMergeHookOwnedFieldsFromDisk(AppSettings target)
    {
        if (!File.Exists(_paths.SettingsFile))
        {
            return;
        }

        var writeUtc = File.GetLastWriteTimeUtc(_paths.SettingsFile);
        if (writeUtc <= _settingsFileWriteUtc)
        {
            return;
        }

        var onDisk = AtomicJsonFile.Read<AppSettings>(_paths.SettingsFile, SettingsJsonOptions.Shared);
        if (onDisk is null)
        {
            return;
        }

        HookOwnedSettingsFields.CopyFrom(onDisk, target);
        _settingsFileWriteUtc = writeUtc;
    }

    private static AppSettings CreateInitialDefaults() => SettingsDefaults.Create();

    private void MigrateLegacySilentAudioIfNeeded(AppSettings settings)
    {
        if (settings.CaptureMicEnabled
            || settings.CaptureDesktopEnabled
            || settings.MicDeviceId.Length > 0
            || settings.DesktopLoopbackDeviceId.Length > 0)
        {
            return;
        }

        settings.CaptureMicEnabled = true;
        settings.CaptureDesktopEnabled = true;
        Save(settings);
    }

    public void Save(AppSettings settings)
    {
        WriteSettings(settings, SettingsSaveMerger.Merge);
    }

    public void SaveFromTray(AppSettings settings)
    {
        WriteSettings(settings, SettingsSaveMerger.MergeForTray);
    }

    private void WriteSettings(
        AppSettings settings,
        Func<AppSettings?, AppSettings, AppSettings> merge)
    {
        AppSettings? onDisk = null;
        if (File.Exists(_paths.SettingsFile))
        {
            onDisk = AtomicJsonFile.Read<AppSettings>(_paths.SettingsFile, SettingsJsonOptions.Shared);
        }

        var merged = merge(onDisk, settings);
        AtomicJsonFile.Write(_paths.SettingsFile, merged, SettingsJsonOptions.Shared);
        RefreshWriteTimestamp();
    }

    private void RefreshWriteTimestamp()
    {
        if (File.Exists(_paths.SettingsFile))
        {
            _settingsFileWriteUtc = File.GetLastWriteTimeUtc(_paths.SettingsFile);
        }
    }
}
