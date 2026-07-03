using Mnemonic.Devices;
using Mnemonic.Ipc;
using Mnemonic.Ipc.Models;
using Mnemonic.Retention;

namespace Mnemonic.Windows.Tray;

internal sealed class SettingsForm : Form
{
    private static readonly Color SeparatorColor = Color.FromArgb(255, 200, 200, 200);
    private static readonly Color BodyBackgroundColor = Color.FromArgb(255, 232, 232, 232);
    private const int FieldRowSpacing = 10;
    private const int FieldRowHeightPx = 28;
    private const int FieldRowTotalHeightPx = FieldRowHeightPx + FieldRowSpacing;
    private const int SectionGapBeforePx = 8;
    private static readonly Padding FieldLabelMargin = new(0, 0, 8, 0);

    private const int RowBody = 0;
    private const int RowFooterSeparator = 1;
    private const int RowButton = 2;
    private readonly SettingsStore _settingsStore;
    private readonly DataRootPaths _paths;
    private readonly AppSettings _settings;
    private readonly HiddenHostForm _host;

    private readonly ComboBox _micCombo;
    private readonly ComboBox _desktopCombo;
    private readonly CheckBox _drawMouseCheck;
    private readonly NumericUpDown _segmentDurationUpDown;
    private readonly NumericUpDown _preserveThresholdUpDown;
    private readonly NumericUpDown _notableScoreMinUpDown;
    private readonly NumericUpDown _highlightScoreMinUpDown;
    private readonly NumericUpDown _segmentHistoryMaxUpDown;
    private readonly Button _saveButton;
    private readonly Panel _buttonPanel;
    private readonly ToolTip _toolTip;
    private AppSettings _baseline = new();
    private int _micIndexAtLoad = -1;
    private int _desktopIndexAtLoad = -1;
    private IReadOnlyList<AudioDeviceOption> _micDevices = [];
    private IReadOnlyList<AudioDeviceOption> _desktopDevices = [];

    public SettingsForm(
        SettingsStore settingsStore,
        DataRootPaths paths,
        AppSettings settings,
        string ffmpegPath,
        HiddenHostForm host)
    {
        _ = ffmpegPath;
        _settingsStore = settingsStore;
        _paths = paths;
        _settings = settings;
        _host = host;
        _toolTip = new ToolTip { AutoPopDelay = 8000, InitialDelay = 400, ReshowDelay = 200 };

        Text = "Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = true;
        Icon = AppBranding.CreateAppIconCopy();
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        AutoScaleDimensions = new SizeF(96F, 96F);
        MinimumSize = new Size(450, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(12, 12, 12, 12),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 3; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        _micCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
        };
        _desktopCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
        };
        _drawMouseCheck = new CheckBox
        {
            Text = "",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Checked = _settings.DrawMouse,
        };
        _segmentDurationUpDown = new NumericUpDown
        {
            Minimum = 30,
            Maximum = 600,
            Increment = 30,
            Width = 100,
            Value = _settings.SegmentDurationSeconds > 0
                ? _settings.SegmentDurationSeconds
                : global::Mnemonic.MnemonicConstants.SegmentDurationSeconds,
        };
        var segmentDurationRow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };
        segmentDurationRow.Controls.Add(_segmentDurationUpDown);
        segmentDurationRow.Controls.Add(CreateInlineSuffixLabel("s"));

        _preserveThresholdUpDown = new NumericUpDown
        {
            Minimum = PreserveThresholdPolicy.Min,
            Maximum = PreserveThresholdPolicy.Max,
            Width = 100,
            Value = _settings.PreserveThreshold > 0
                ? _settings.PreserveThreshold
                : SettingsDefaults.Create().PreserveThreshold,
        };
        _toolTip.SetToolTip(
            _preserveThresholdUpDown,
            "Minimum score to auto-save at segment end. "
            + "Save segment is only offered when the current score is below this threshold.");

        _notableScoreMinUpDown = new NumericUpDown
        {
            Minimum = NotableScoreMinPolicy.Min,
            Maximum = NotableScoreMinPolicy.Max,
            Width = 100,
            Value = _settings.NotableScoreMin > 0
                ? _settings.NotableScoreMin
                : SettingsDefaults.Create().NotableScoreMin,
        };
        _toolTip.SetToolTip(
            _notableScoreMinUpDown,
            "Minimum score for Notable tier in the Mnemonic dock (tooltips, filter, badges). "
            + "Does not change auto-save; only how clips are labeled.");

        _highlightScoreMinUpDown = new NumericUpDown
        {
            Minimum = HighlightScoreMinPolicy.Min,
            Maximum = HighlightScoreMinPolicy.Max,
            Width = 100,
            Value = _settings.HighlightScoreMin > 0
                ? _settings.HighlightScoreMin
                : global::Mnemonic.MnemonicConstants.SignificanceTierHighlightScoreMin,
        };
        _toolTip.SetToolTip(
            _highlightScoreMinUpDown,
            "Minimum score for Highlight tier in the Mnemonic dock (tooltips, filter, badges). "
            + "Does not change auto-save; only how clips are labeled.");

        _segmentHistoryMaxUpDown = new NumericUpDown
        {
            Minimum = SegmentHistoryMaxEntriesPolicy.Min,
            Maximum = SegmentHistoryMaxEntriesPolicy.Max,
            Width = 100,
            Value = _settings.SegmentHistoryMaxEntries > 0
                ? _settings.SegmentHistoryMaxEntries
                : SegmentHistoryMaxEntriesPolicy.Default,
        };
        _toolTip.SetToolTip(
            _segmentHistoryMaxUpDown,
            "Maximum segment-close records kept in the segment log. Older entries are removed when the limit is exceeded.");

        _toolTip.SetToolTip(
            _drawMouseCheck,
            "Include the mouse cursor in screen capture. Takes effect after Save and restart.");

        var bodyLayout = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 10,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BodyBackgroundColor,
            Padding = new Padding(0, 8, 0, 8),
            Margin = Padding.Empty,
        };
        bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 10; i++)
        {
            bodyLayout.RowStyles.Add(
                i is 0 or 3
                    ? new RowStyle(SizeType.AutoSize)
                    : new RowStyle(SizeType.Absolute, FieldRowTotalHeightPx));
        }

        var audioHeading = CreateSectionHeading("Audio");
        bodyLayout.SetColumnSpan(audioHeading, 2);
        bodyLayout.Controls.Add(audioHeading, 0, 0);

        bodyLayout.Controls.Add(CreateFieldLabel("Microphone:"), 0, 1);
        bodyLayout.Controls.Add(WrapFieldControl(_micCombo), 1, 1);
        bodyLayout.Controls.Add(CreateFieldLabel("Desktop:"), 0, 2);
        bodyLayout.Controls.Add(WrapFieldControl(_desktopCombo), 1, 2);

        var retentionHeading = CreateSectionHeading("Retention", SectionGapBeforePx);
        bodyLayout.SetColumnSpan(retentionHeading, 2);
        bodyLayout.Controls.Add(retentionHeading, 0, 3);
        _toolTip.SetToolTip(
            retentionHeading,
            "Segment length, auto-save threshold, and capture options. "
            + "Some options need a new recording session.");

        bodyLayout.Controls.Add(CreateFieldLabel("Segment length (s):"), 0, 4);
        bodyLayout.Controls.Add(WrapFieldControl(segmentDurationRow), 1, 4);
        _toolTip.SetToolTip(
            _segmentDurationUpDown,
            "How long each segment runs before it closes.");
        bodyLayout.Controls.Add(CreateFieldLabel("Preserve threshold:"), 0, 5);
        bodyLayout.Controls.Add(WrapFieldControl(_preserveThresholdUpDown), 1, 5);
        bodyLayout.Controls.Add(CreateFieldLabel("Notable score:"), 0, 6);
        bodyLayout.Controls.Add(WrapFieldControl(_notableScoreMinUpDown), 1, 6);
        bodyLayout.Controls.Add(CreateFieldLabel("Highlight score:"), 0, 7);
        bodyLayout.Controls.Add(WrapFieldControl(_highlightScoreMinUpDown), 1, 7);
        bodyLayout.Controls.Add(CreateFieldLabel("Segment log retention:"), 0, 8);
        bodyLayout.Controls.Add(WrapFieldControl(_segmentHistoryMaxUpDown), 1, 8);
        bodyLayout.Controls.Add(CreateFieldLabel("Capture cursor:"), 0, 9);
        bodyLayout.Controls.Add(WrapFieldControl(_drawMouseCheck), 1, 9);

        var bodyPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BodyBackgroundColor,
            Margin = Padding.Empty,
        };
        bodyPanel.Controls.Add(bodyLayout);
        layout.SetColumnSpan(bodyPanel, 2);
        layout.Controls.Add(bodyPanel, 0, RowBody);

        var footerSeparator = CreateHorizontalRule(margin: new Padding(0, FieldRowSpacing, 0, FieldRowSpacing));
        layout.SetColumnSpan(footerSeparator, 2);
        layout.Controls.Add(footerSeparator, 0, RowFooterSeparator);

        _buttonPanel = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 0, 0),
            MinimumSize = new Size(0, 36),
        };
        _saveButton = CreateDialogButton("Save and restart");
        _saveButton.Enabled = false;
        _saveButton.Anchor = AnchorStyles.None;
        _saveButton.Click += OnSaveClick;
        _buttonPanel.Controls.Add(_saveButton);
        _buttonPanel.Resize += (_, _) => CenterControl(_buttonPanel, _saveButton);
        layout.SetColumnSpan(_buttonPanel, 2);
        layout.Controls.Add(_buttonPanel, 0, RowButton);

        Controls.Add(layout);

        AcceptButton = _saveButton;

        _micCombo.SelectedIndexChanged += (_, _) =>
        {
            UpdateComboSelectionTooltip(_micCombo, _micDevices);
            UpdateSaveButtonState();
        };
        _desktopCombo.SelectedIndexChanged += (_, _) =>
        {
            UpdateComboSelectionTooltip(_desktopCombo, _desktopDevices);
            UpdateSaveButtonState();
        };
        _drawMouseCheck.CheckedChanged += (_, _) => UpdateSaveButtonState();
        _segmentDurationUpDown.ValueChanged += (_, _) => UpdateSaveButtonState();
        _preserveThresholdUpDown.ValueChanged += (_, _) => UpdateSaveButtonState();
        _notableScoreMinUpDown.ValueChanged += (_, _) => UpdateSaveButtonState();
        _highlightScoreMinUpDown.ValueChanged += (_, _) => UpdateSaveButtonState();
        _segmentHistoryMaxUpDown.ValueChanged += (_, _) => UpdateSaveButtonState();

        Load += OnFormLoad;
    }

    private void OnFormLoad(object? sender, EventArgs e)
    {
        _micCombo.Enabled = false;
        _desktopCombo.Enabled = false;
        UseWaitCursor = true;

        try
        {
            _micDevices = WasapiDeviceLister.ListCaptureDevices();
            _desktopDevices = WasapiDeviceLister.ListRenderDevices();
            PopulateCombo(_micCombo, _micDevices, _settings.MicDeviceId);
            PopulateCombo(_desktopCombo, _desktopDevices, _settings.DesktopLoopbackDeviceId);
            ConfigureComboDropdowns();
            UpdateComboSelectionTooltip(_micCombo, _micDevices);
            UpdateComboSelectionTooltip(_desktopCombo, _desktopDevices);
            _micIndexAtLoad = _micCombo.SelectedIndex;
            _desktopIndexAtLoad = _desktopCombo.SelectedIndex;
            _drawMouseCheck.Checked = _settings.DrawMouse;
            _segmentDurationUpDown.Value = _settings.SegmentDurationSeconds > 0
                ? _settings.SegmentDurationSeconds
                : global::Mnemonic.MnemonicConstants.SegmentDurationSeconds;
            _preserveThresholdUpDown.Value = _settings.PreserveThreshold > 0
                ? _settings.PreserveThreshold
                : SettingsDefaults.Create().PreserveThreshold;
            _notableScoreMinUpDown.Value = _settings.NotableScoreMin > 0
                ? _settings.NotableScoreMin
                : SettingsDefaults.Create().NotableScoreMin;
            _highlightScoreMinUpDown.Value = _settings.HighlightScoreMin > 0
                ? _settings.HighlightScoreMin
                : global::Mnemonic.MnemonicConstants.SignificanceTierHighlightScoreMin;
            _segmentHistoryMaxUpDown.Value = _settings.SegmentHistoryMaxEntries > 0
                ? _settings.SegmentHistoryMaxEntries
                : SegmentHistoryMaxEntriesPolicy.Default;
            _baseline = SnapshotBaseline();
            UpdateSaveButtonState();
            CenterControl(_buttonPanel, _saveButton);
            FitClientSizeToLayout();
        }
        finally
        {
            _micCombo.Enabled = true;
            _desktopCombo.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void FitClientSizeToLayout()
    {
        if (Controls.Count == 0 || Controls[0] is not TableLayoutPanel layout)
        {
            return;
        }

        layout.PerformLayout();
        var preferred = layout.GetPreferredSize(Size.Empty);
        var padding = layout.Padding;
        ClientSize = new Size(
            Math.Max(450, preferred.Width + padding.Horizontal),
            preferred.Height + padding.Vertical);
        CenterControl(_buttonPanel, _saveButton);
    }

    private void ConfigureComboDropdowns()
    {
        var labelColumnWidth = 160;
        var dropdownWidth = Math.Max(280, ClientSize.Width - labelColumnWidth - 24);
        _micCombo.DropDownWidth = dropdownWidth;
        _desktopCombo.DropDownWidth = dropdownWidth;
    }

    private static void PopulateCombo(ComboBox combo, IReadOnlyList<AudioDeviceOption> devices, string selectedId)
    {
        combo.Items.Clear();
        foreach (var device in devices)
        {
            combo.Items.Add(device);
        }

        combo.DisplayMember = nameof(AudioDeviceOption.DisplayName);
        var selectedIndex = AudioDeviceSelection.FindIndexForId(devices, selectedId);
        combo.SelectedIndex = selectedIndex;
    }

    private void UpdateComboSelectionTooltip(ComboBox combo, IReadOnlyList<AudioDeviceOption> devices)
    {
        if (!AudioDeviceSelection.TryGetDeviceAt(devices, combo.SelectedIndex, out var device))
        {
            _toolTip.SetToolTip(combo, string.Empty);
            return;
        }

        _toolTip.SetToolTip(combo, device.DisplayName);
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        ApplySettingsFromUi();
        SettingsSanitizer.Sanitize(_settings);
        _settingsStore.SaveFromTray(_settings);
        new SegmentHistoryStore().TrimToMax(
            _paths.SegmentHistoryFile,
            _settings.SegmentHistoryMaxEntries);
        _host.RestartCapture();
        _micIndexAtLoad = _micCombo.SelectedIndex;
        _desktopIndexAtLoad = _desktopCombo.SelectedIndex;
        _baseline = SnapshotBaseline();
        UpdateSaveButtonState();
        DialogResult = DialogResult.OK;
    }

    private void ApplySettingsFromUi()
    {
        SettingsFormApply.ApplyTrayFieldsFromUi(_settings, SettingsFromUi());
    }

    private AppSettings SettingsFromUi()
    {
        var result = AppSettingsCaptureBinding.Snapshot(_settings);
        ApplyAudioSelections(result);
        result.DrawMouse = _drawMouseCheck.Checked;
        result.SegmentDurationSeconds = (int)_segmentDurationUpDown.Value;
        result.PreserveThreshold = (int)_preserveThresholdUpDown.Value;
        result.NotableScoreMin = (int)_notableScoreMinUpDown.Value;
        result.HighlightScoreMin = (int)_highlightScoreMinUpDown.Value;
        result.SegmentHistoryMaxEntries = (int)_segmentHistoryMaxUpDown.Value;
        return result;
    }

    private AppSettings SnapshotBaseline()
    {
        var snapshot = AppSettingsCaptureBinding.Snapshot(_settings);
        snapshot.SegmentDurationSeconds = _settings.SegmentDurationSeconds;
        snapshot.PreserveThreshold = _settings.PreserveThreshold;
        snapshot.NotableScoreMin = _settings.NotableScoreMin;
        snapshot.SegmentHistoryMaxEntries = _settings.SegmentHistoryMaxEntries;
        snapshot.HighlightScoreMin = _settings.HighlightScoreMin;
        snapshot.DrawMouse = _settings.DrawMouse;
        return snapshot;
    }

    private void ApplyAudioSelections(AppSettings result)
    {
        if (_micCombo.SelectedIndex != _micIndexAtLoad)
        {
            if (AudioDeviceSelection.TryGetDeviceAt(_micDevices, _micCombo.SelectedIndex, out var mic))
            {
                AudioDeviceSelection.ApplyMicSelection(result, mic);
            }
            else
            {
                AudioDeviceSelection.ClearMicSelection(result);
            }
        }

        if (_desktopCombo.SelectedIndex != _desktopIndexAtLoad)
        {
            if (AudioDeviceSelection.TryGetDeviceAt(_desktopDevices, _desktopCombo.SelectedIndex, out var desktop))
            {
                AudioDeviceSelection.ApplyDesktopSelection(result, desktop);
            }
            else
            {
                AudioDeviceSelection.ClearDesktopSelection(result);
            }
        }
    }

    private void UpdateSaveButtonState()
    {
        var fromUi = SettingsFromUi();
        var dirty = !AppSettingsCaptureBinding.CaptureFieldsEqual(_baseline, fromUi)
            || _baseline.DrawMouse != fromUi.DrawMouse
            || _baseline.SegmentDurationSeconds != fromUi.SegmentDurationSeconds
            || _baseline.PreserveThreshold != fromUi.PreserveThreshold
            || _baseline.NotableScoreMin != fromUi.NotableScoreMin
            || _baseline.HighlightScoreMin != fromUi.HighlightScoreMin
            || _baseline.SegmentHistoryMaxEntries != fromUi.SegmentHistoryMaxEntries;
        _saveButton.Enabled = dirty;
        AcceptButton = dirty ? _saveButton : null;
    }

    private static void CenterControl(Control container, Control child)
    {
        child.Location = new Point(
            Math.Max(0, (container.ClientSize.Width - child.Width) / 2),
            Math.Max(0, (container.ClientSize.Height - child.Height) / 2));
    }

    private static Panel WrapFieldControl(Control control, Padding margin = default)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = margin == default ? Padding.Empty : margin,
            BackColor = Color.Transparent,
        };
        control.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        panel.Controls.Add(control);
        void CenterVertically(object? _, EventArgs __) =>
            control.Location = new Point(
                0,
                Math.Max(0, (panel.ClientSize.Height - control.Height) / 2));
        panel.Resize += CenterVertically;
        panel.HandleCreated += (_, _) => CenterVertically(panel, EventArgs.Empty);
        return panel;
    }

    private static Panel CreateHorizontalRule(Padding margin) =>
        new()
        {
            Dock = DockStyle.Fill,
            Margin = margin,
            BackColor = SeparatorColor,
            MinimumSize = new Size(0, 1),
            MaximumSize = new Size(10000, 1),
        };

    private Label CreateSectionHeading(string text, int gapBeforePx = 0) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = Font,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, gapBeforePx, 0, 4),
            BackColor = Color.Transparent,
            UseCompatibleTextRendering = false,
        };

    private Label CreateFieldLabel(string text) =>
        new()
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = SystemColors.ControlText,
            Margin = FieldLabelMargin,
            BackColor = Color.Transparent,
            UseCompatibleTextRendering = false,
        };

    private Label CreateInlineSuffixLabel(string text) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(4, 0, 0, 0),
            ForeColor = SystemColors.GrayText,
            BackColor = Color.Transparent,
            UseCompatibleTextRendering = false,
        };

    private Button CreateDialogButton(string text, DialogResult dialogResult = DialogResult.None)
    {
        var size = MeasureButtonSize(text, Font);
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Size = size,
            MinimumSize = size,
            Margin = new Padding(0),
            UseCompatibleTextRendering = false,
            DialogResult = dialogResult,
        };
        return button;
    }

    private static Size MeasureButtonSize(string text, Font font)
    {
        var displayText = text.Replace("&&", "&", StringComparison.Ordinal);
        var textSize = TextRenderer.MeasureText(
            displayText,
            font,
            Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.TextBoxControl);
        var width = textSize.Width + SystemInformation.HorizontalFocusThickness * 2 + 24;
        var height = textSize.Height + SystemInformation.VerticalFocusThickness * 2 + 14;
        return new Size(Math.Max(width, 80), Math.Max(height, 28));
    }
}
