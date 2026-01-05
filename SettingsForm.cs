using System;
using System.Drawing;
using System.Windows.Forms;
using RecPlay.Hotkeys;
using RecPlay.Models;

namespace RecPlay;

internal sealed class SettingsForm : Form
{
    private readonly HotkeyBox _recordHotkeyBox = new();
    private readonly HotkeyBox _playbackHotkeyBox = new();
    private readonly NumericUpDown _repeatCount = new();
    private readonly NumericUpDown _intervalMs = new();
    private readonly NumericUpDown _playbackSpeed = new();
    private readonly Button _saveButton = new();
    private readonly Button _cancelButton = new();

    public SettingsForm(UserSettings settings)
    {
        Text = "RecPlay Settings";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(460, 300);
        ClientSize = new Size(520, 320);

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(12),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var recordLabel = new Label { Text = "Record hotkey", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        var playbackLabel = new Label { Text = "Playback hotkey", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        var repeatLabel = new Label { Text = "Repeat count", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        var intervalLabel = new Label { Text = "Interval (ms)", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        var speedLabel = new Label { Text = "Playback speed", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };

        _recordHotkeyBox.Dock = DockStyle.Fill;
        _playbackHotkeyBox.Dock = DockStyle.Fill;
        _recordHotkeyBox.ShortcutsEnabled = false;
        _playbackHotkeyBox.ShortcutsEnabled = false;

        _repeatCount.Minimum = 1;
        _repeatCount.Maximum = 1000;
        _repeatCount.Anchor = AnchorStyles.Left;
        _repeatCount.Width = 160;

        _intervalMs.Minimum = 0;
        _intervalMs.Maximum = 600000;
        _intervalMs.Anchor = AnchorStyles.Left;
        _intervalMs.Width = 160;

        _playbackSpeed.Minimum = 0.5M;
        _playbackSpeed.Maximum = 5.0M;
        _playbackSpeed.DecimalPlaces = 1;
        _playbackSpeed.Increment = 0.1M;
        _playbackSpeed.Anchor = AnchorStyles.Left;
        _playbackSpeed.Width = 160;

        table.Controls.Add(recordLabel, 0, 0);
        table.Controls.Add(_recordHotkeyBox, 1, 0);
        table.Controls.Add(playbackLabel, 0, 1);
        table.Controls.Add(_playbackHotkeyBox, 1, 1);
        table.Controls.Add(repeatLabel, 0, 2);
        table.Controls.Add(_repeatCount, 1, 2);
        table.Controls.Add(intervalLabel, 0, 3);
        table.Controls.Add(_intervalMs, 1, 3);
        table.Controls.Add(speedLabel, 0, 4);
        table.Controls.Add(_playbackSpeed, 1, 4);

        var spacer = new Panel
        {
            Dock = DockStyle.Fill,
        };

        var buttonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };

        _saveButton.Text = "Save";
        _saveButton.AutoSize = true;
        _saveButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _saveButton.Padding = new Padding(8, 4, 8, 4);
        _saveButton.Click += SaveButton_Click;
        _cancelButton.Text = "Cancel";
        _cancelButton.AutoSize = true;
        _cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _cancelButton.Padding = new Padding(8, 4, 8, 4);
        _cancelButton.Click += (_, _) => Close();

        buttonPanel.Controls.Add(_saveButton);
        buttonPanel.Controls.Add(_cancelButton);
        table.Controls.Add(spacer, 0, 5);
        table.SetColumnSpan(spacer, 2);
        table.Controls.Add(buttonPanel, 0, 6);
        table.SetColumnSpan(buttonPanel, 2);

        Controls.Add(table);

        LoadSettings(settings);
        ApplyWindowSizing(table, settings);
    }

    public UserSettings Settings { get; private set; } = new();
    public Size LastSize { get; private set; }

    private void LoadSettings(UserSettings settings)
    {
        Settings = settings;

        if (Hotkey.TryParse(settings.RecordHotkey, out var recordHotkey))
        {
            _recordHotkeyBox.Hotkey = recordHotkey;
        }

        if (Hotkey.TryParse(settings.PlaybackHotkey, out var playbackHotkey))
        {
            _playbackHotkeyBox.Hotkey = playbackHotkey;
        }

        _repeatCount.Value = Math.Clamp(settings.RepeatCount, 1, 1000);
        _intervalMs.Value = Math.Clamp(settings.IntervalMs, 0, 600000);
        _playbackSpeed.Value = (decimal)Math.Clamp(settings.PlaybackSpeed, 0.5, 5.0);
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (_recordHotkeyBox.Hotkey.IsEmpty || _playbackHotkeyBox.Hotkey.IsEmpty)
        {
            MessageBox.Show(this, "Please set both hotkeys.", "RecPlay", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Settings = new UserSettings
        {
            RecordHotkey = _recordHotkeyBox.Hotkey.ToString(),
            PlaybackHotkey = _playbackHotkeyBox.Hotkey.ToString(),
            RepeatCount = (int)_repeatCount.Value,
            IntervalMs = (int)_intervalMs.Value,
            PlaybackSpeed = (double)_playbackSpeed.Value,
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        LastSize = Size;
        base.OnFormClosing(e);
    }

    private void ApplyWindowSizing(TableLayoutPanel table, UserSettings settings)
    {
        table.PerformLayout();
        var preferred = table.PreferredSize;
        var chromeSize = new Size(Width - ClientSize.Width, Height - ClientSize.Height);

        var minClientWidth = Math.Max(ClientSize.Width, preferred.Width);
        var minClientHeight = Math.Max(ClientSize.Height, preferred.Height);
        var minSize = new Size(minClientWidth + chromeSize.Width, minClientHeight + chromeSize.Height);

        MinimumSize = new Size(
            Math.Max(MinimumSize.Width, minSize.Width),
            Math.Max(MinimumSize.Height, minSize.Height));
        ClientSize = new Size(minClientWidth, minClientHeight);

        if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
        {
            var desired = new Size(settings.WindowWidth, settings.WindowHeight);
            Size = new Size(
                Math.Max(desired.Width, MinimumSize.Width),
                Math.Max(desired.Height, MinimumSize.Height));
        }
    }
}
