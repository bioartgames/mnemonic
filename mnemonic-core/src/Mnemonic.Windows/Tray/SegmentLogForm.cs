using Mnemonic.Display;
using Mnemonic.Ipc;
using Mnemonic.Retention;

namespace Mnemonic.Windows.Tray;

internal sealed class SegmentLogForm : Form
{
    private readonly DataRootPaths _paths;
    private readonly SegmentHistoryStore _store = new();
    private readonly ListBox _list;
    private readonly Button _clearButton;
    private readonly ToolTip _toolTip;
    private IReadOnlyList<SegmentHistoryRecord> _records = [];

    private const string EmptyHistoryMessage = "No segments yet.";

    private const string MissingHistoryFileMessage = "No segment history yet.";

    public SegmentLogForm(DataRootPaths paths)
    {
        _paths = paths;

        Text = "Segment log";
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        ShowIcon = true;
        Icon = AppBranding.CreateAppIconCopy();
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(520, 320);
        Size = new Size(640, 480);

        _toolTip = new ToolTip { AutoPopDelay = 12000, InitialDelay = 400, ReshowDelay = 200 };

        _list = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            HorizontalScrollbar = true,
            Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point),
        };
        _list.MouseMove += OnListMouseMove;

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(8),
        };

        _clearButton = new Button { Text = "Clear log", AutoSize = true };
        _clearButton.Click += OnClearClick;
        var refreshButton = new Button { Text = "Refresh", AutoSize = true };
        refreshButton.Click += (_, _) => ReloadList();

        buttonPanel.Controls.Add(_clearButton);
        buttonPanel.Controls.Add(refreshButton);

        Controls.Add(_list);
        Controls.Add(buttonPanel);

        Shown += (_, _) => ReloadList();
    }

    private void ReloadList()
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        _records = _store.ReadAllNewestFirst(_paths.SegmentHistoryFile);
        if (_records.Count == 0)
        {
            _list.Items.Add(
                File.Exists(_paths.SegmentHistoryFile)
                    ? EmptyHistoryMessage
                    : MissingHistoryFileMessage);
        }
        else
        {
            foreach (var record in _records)
            {
                _list.Items.Add(FormatSummary(record));
            }
        }

        _list.EndUpdate();
        _clearButton.Enabled = _records.Count > 0;
    }

    private void OnListMouseMove(object? sender, MouseEventArgs e)
    {
        if (_records.Count == 0)
        {
            _toolTip.SetToolTip(_list, string.Empty);
            return;
        }

        var index = _list.IndexFromPoint(e.Location);
        if (index < 0 || index >= _records.Count)
        {
            _toolTip.SetToolTip(_list, string.Empty);
            return;
        }

        _toolTip.SetToolTip(_list, FormatTooltip(_records[index]));
    }

    private static string FormatSummary(SegmentHistoryRecord record)
    {
        var outcome = record.Preserved ? "kept" : "discarded";
        var timeRange = DisplayTimeFormat.FormatLocalTimeRange(
            (long)record.TOpenUnix,
            (long)record.TCloseUnix);
        var commit = record.GitCommit.Length > 7 ? record.GitCommit[..7] : record.GitCommit;
        var branch = string.IsNullOrWhiteSpace(record.GitBranch) ? "—" : record.GitBranch;
        return
            $"#{record.SegmentIndex:D5} · {outcome} · score {record.Score} (threshold {record.Threshold}) · {timeRange} · {branch}@{commit}";
    }

    private static string FormatTooltip(SegmentHistoryRecord record)
    {
        var lines = new List<string>
        {
            $"capture: {record.CapturePrefix}",
            $"manual preserve: {(record.ManualPreserve ? "yes" : "no")}",
        };
        if (!string.IsNullOrWhiteSpace(record.ClipId))
        {
            lines.Add($"clip: {record.ClipId}");
        }

        if (!string.IsNullOrWhiteSpace(record.GitSubject))
        {
            lines.Add($"subject: {record.GitSubject}");
        }

        foreach (var line in record.Breakdown)
        {
            var summary = $"{line.Type}: {line.Count} (+{line.Points})";
            lines.Add(string.IsNullOrWhiteSpace(line.Detail) ? summary : $"{summary} — {line.Detail}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private void OnClearClick(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            this,
            "Delete all segment history entries?",
            "Clear segment log",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (result != DialogResult.Yes)
        {
            return;
        }

        _store.Clear(_paths.SegmentHistoryFile);
        ReloadList();
    }
}
