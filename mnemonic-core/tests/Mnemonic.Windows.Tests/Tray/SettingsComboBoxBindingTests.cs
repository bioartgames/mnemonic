using Mnemonic.Devices;
using Xunit;

namespace Mnemonic.Windows.Tests.Tray;

public sealed class SettingsComboBoxBindingTests
{
    private static readonly IReadOnlyList<AudioDeviceOption> SampleDevices =
    [
        new("", "(System default)"),
        new("{0.0.1.00000000}.{abcdef12-3456-7890-abcd-ef1234567890}", "Microphone (Realtek(R) Audio)"),
    ];

    [Fact]
    public void ComboBox_selected_index_reads_realtek_device_while_selected_item_pattern_can_fail()
    {
        Assert.True(OperatingSystem.IsWindows());

        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var combo = CreatePopulatedCombo(SampleDevices, selectedIndex: 1);

                Assert.True(AudioDeviceSelection.TryGetDeviceAt(
                    SampleDevices,
                    combo.SelectedIndex,
                    out var viaIndex));
                Assert.Equal(SampleDevices[1].Id, viaIndex.Id);

                var selectedItemWorks = combo.SelectedItem is AudioDeviceOption viaItem
                    && viaItem.Id == SampleDevices[1].Id;
                if (!selectedItemWorks)
                {
                    Assert.Fail(
                        "SelectedItem did not yield AudioDeviceOption; use SelectedIndex + device list instead.");
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }

    [Fact]
    public void ComboBox_restore_saved_id_via_AudioDeviceSelection_round_trips_realtek_row()
    {
        Assert.True(OperatingSystem.IsWindows());

        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var savedId = SampleDevices[1].Id;
                var index = AudioDeviceSelection.FindIndexForId(SampleDevices, savedId);
                using var combo = CreatePopulatedCombo(SampleDevices, index);

                Assert.Equal(index, combo.SelectedIndex);
                Assert.True(AudioDeviceSelection.TryGetDeviceAt(
                    SampleDevices,
                    combo.SelectedIndex,
                    out var device));
                Assert.Equal(savedId, device.Id);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }

    private static ComboBox CreatePopulatedCombo(IReadOnlyList<AudioDeviceOption> devices, int selectedIndex)
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var device in devices)
        {
            combo.Items.Add(device);
        }

        combo.DisplayMember = nameof(AudioDeviceOption.DisplayName);
        combo.SelectedIndex = devices.Count > 0 ? selectedIndex : -1;
        return combo;
    }
}
