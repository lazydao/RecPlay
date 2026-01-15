using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RecPlay.Hotkeys;
using RecPlay.Models;
using RecPlay.Native;
using RecPlay.Services;

namespace RecPlay;

internal sealed class TrayAppContext : ApplicationContext
{
    private const int HotkeyRecordId = 1;
    private const int HotkeyPlaybackId = 2;

    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _recordMenuItem;
    private readonly ToolStripMenuItem _playMenuItem;
    private readonly Icon _idleIcon;
    private readonly Icon _recordingIcon;
    private readonly Icon _replayingIcon;

    private readonly SettingsService _settingsService = new();
    private readonly RecordingStorage _recordingStorage = new();
    private readonly Recorder _recorder = new();
    private readonly Player _player = new();
    private readonly HotkeyWindow _hotkeyWindow = new();

    private UserSettings _settings;
    private IReadOnlyList<InputEvent> _recording = new List<InputEvent>();
    private CancellationTokenSource? _playbackCts;
    private bool _isPlaying;
    private Hotkey _recordHotkey;
    private Hotkey _playbackHotkey;

    public TrayAppContext()
    {
        _idleIcon = LoadTrayIcon("Idle.png");
        _recordingIcon = LoadTrayIcon("Recording.png");
        _replayingIcon = LoadTrayIcon("Replaying.png");

        _settings = _settingsService.Load();
        _recording = _recordingStorage.Load();

        _trayIcon = new NotifyIcon
        {
            Icon = _idleIcon,
            Text = "RecPlay",
            Visible = true,
        };

        _recordMenuItem = new ToolStripMenuItem("Start Recording", null, (_, _) => ToggleRecording());
        _playMenuItem = new ToolStripMenuItem("Playback", null, async (_, _) => await TogglePlaybackAsync());
        var settingsItem = new ToolStripMenuItem("Settings...", null, (_, _) => ShowSettings());
        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitApp());

        var menu = new ContextMenuStrip();
        menu.Items.Add(_recordMenuItem);
        menu.Items.Add(_playMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenuStrip = menu;

        _hotkeyWindow.HotkeyPressed += HotkeyWindowOnHotkeyPressed;
        if (!RegisterFromSettings(_settings, out var error))
        {
            ShowBalloon("Hotkey registration failed", error, ToolTipIcon.Warning);
        }

        UpdateMenuState();
    }

    private void HotkeyWindowOnHotkeyPressed(object? sender, int id)
    {
        if (id == HotkeyRecordId)
        {
            ToggleRecording();
        }
        else if (id == HotkeyPlaybackId)
        {
            _ = TogglePlaybackAsync();
        }
    }

    private void ToggleRecording()
    {
        if (_recorder.IsRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    private void StartRecording()
    {
        StopPlayback();
        if (!_recorder.Start())
        {
            ShowBalloon("Recording failed", "Unable to register input hooks.", ToolTipIcon.Warning);
            return;
        }
        _trayIcon.Text = "RecPlay (Recording)";
        UpdateMenuState();
        ShowBalloon("Recording started", "Press the record hotkey again to stop.", ToolTipIcon.Info);
    }

    private void StopRecording()
    {
        _recording = _recorder.Stop();
        _recordingStorage.Save(_recording);
        _trayIcon.Text = "RecPlay";
        UpdateMenuState();
        ShowBalloon("Recording saved", $"Events: {_recording.Count}", ToolTipIcon.Info);
    }

    private async Task TogglePlaybackAsync()
    {
        if (_isPlaying)
        {
            StopPlayback();
            return;
        }

        if (_recording.Count == 0)
        {
            ShowBalloon("No recording found", "Record something first.", ToolTipIcon.Info);
            return;
        }

        if (_recorder.IsRecording)
        {
            StopRecording();
        }

        _isPlaying = true;
        _trayIcon.Text = "RecPlay (Playback)";
        UpdateMenuState();

        _playbackCts = new CancellationTokenSource();
        try
        {
            await _player.PlayAsync(_recording, _settings.RepeatCount, _settings.IntervalMs, _settings.PlaybackSpeed, _playbackCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _isPlaying = false;
            _playbackCts?.Dispose();
            _playbackCts = null;
            _trayIcon.Text = "RecPlay";
            UpdateMenuState();
        }
    }

    private void StopPlayback()
    {
        if (_playbackCts == null)
        {
            return;
        }

        _playbackCts.Cancel();
        _playbackCts.Dispose();
        _playbackCts = null;
    }

    private void ShowSettings()
    {
        _hotkeyWindow.UnregisterAll();
        using var form = new SettingsForm(_settings);
        var result = form.ShowDialog();
        var windowSize = form.LastSize;

        if (result == DialogResult.OK)
        {
            var newSettings = form.Settings;
            TryUpdateWindowSize(newSettings, windowSize);
            if (!RegisterFromSettings(newSettings, out var error))
            {
                RegisterFromSettings(_settings, out _);
                PersistWindowSize(windowSize);
                ShowBalloon("Hotkey registration failed", error, ToolTipIcon.Warning);
                return;
            }

            _settings = newSettings;
            _settingsService.Save(_settings);
            ShowBalloon("Settings saved", "Hotkeys and playback settings updated.", ToolTipIcon.Info);
            return;
        }

        RegisterFromSettings(_settings, out var registerError);
        PersistWindowSize(windowSize);
        if (!string.IsNullOrEmpty(registerError))
        {
            ShowBalloon("Hotkey registration failed", registerError, ToolTipIcon.Warning);
        }
    }

    private bool RegisterFromSettings(UserSettings settings, out string error)
    {
        error = string.Empty;
        if (!Hotkey.TryParse(settings.RecordHotkey, out var recordHotkey))
        {
            error = "Invalid record hotkey.";
            return false;
        }
        if (!Hotkey.TryParse(settings.PlaybackHotkey, out var playbackHotkey))
        {
            error = "Invalid playback hotkey.";
            return false;
        }

        _hotkeyWindow.UnregisterHotkey(HotkeyRecordId);
        _hotkeyWindow.UnregisterHotkey(HotkeyPlaybackId);

        if (!_hotkeyWindow.RegisterHotkey(HotkeyRecordId, recordHotkey))
        {
            error = "Record hotkey is already in use.";
            return false;
        }
        if (!_hotkeyWindow.RegisterHotkey(HotkeyPlaybackId, playbackHotkey))
        {
            _hotkeyWindow.UnregisterHotkey(HotkeyRecordId);
            error = "Playback hotkey is already in use.";
            return false;
        }

        _recordHotkey = recordHotkey;
        _playbackHotkey = playbackHotkey;
        return true;
    }

    private void UpdateMenuState()
    {
        _recordMenuItem.Text = _recorder.IsRecording ? "Stop Recording" : "Start Recording";
        _playMenuItem.Text = _isPlaying ? "Stop Playback" : "Playback";
        UpdateTrayIcon();
    }

    private void ShowBalloon(string title, string message, ToolTipIcon icon)
    {
        _trayIcon.ShowBalloonTip(1500, title, message, icon);
    }

    private static bool TryUpdateWindowSize(UserSettings settings, Size size)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            return false;
        }

        if (settings.WindowWidth == size.Width && settings.WindowHeight == size.Height)
        {
            return false;
        }

        settings.WindowWidth = size.Width;
        settings.WindowHeight = size.Height;
        return true;
    }

    private void PersistWindowSize(Size size)
    {
        if (TryUpdateWindowSize(_settings, size))
        {
            _settingsService.Save(_settings);
        }
    }

    private void ExitApp()
    {
        StopPlayback();
        if (_recorder.IsRecording)
        {
            StopRecording();
        }

        _hotkeyWindow.Dispose();
        _recorder.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _idleIcon.Dispose();
        _recordingIcon.Dispose();
        _replayingIcon.Dispose();

        ExitThread();
    }

    private void UpdateTrayIcon()
    {
        if (_isPlaying)
        {
            _trayIcon.Icon = _replayingIcon;
            return;
        }

        _trayIcon.Icon = _recorder.IsRecording ? _recordingIcon : _idleIcon;
    }

    private static Icon LoadTrayIcon(string fileName)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, fileName);
            if (!File.Exists(path))
            {
                return GetDefaultTrayIcon();
            }

            using var source = new Bitmap(path);
            var hasTransparency = HasTransparentPixels(source);
            using var masked = hasTransparency
                ? source.Clone(new Rectangle(0, 0, source.Width, source.Height), PixelFormat.Format32bppArgb)
                : ApplyBackgroundMask(source, SampleBackgroundColors(source));
            var bounds = GetLargestOpaqueBounds(masked);
            using var cropped = bounds.IsEmpty
                ? masked.Clone(new Rectangle(0, 0, masked.Width, masked.Height), PixelFormat.Format32bppArgb)
                : masked.Clone(bounds, PixelFormat.Format32bppArgb);
            var targetSize = GetTargetIconSize();
            using var resized = new Bitmap(targetSize, targetSize);
            using (var graphics = Graphics.FromImage(resized))
            {
                var scale = Math.Min(
                    targetSize / (float)cropped.Width,
                    targetSize / (float)cropped.Height);
                var drawWidth = (int)Math.Round(cropped.Width * scale);
                var drawHeight = (int)Math.Round(cropped.Height * scale);
                var offsetX = (targetSize - drawWidth) / 2;
                var offsetY = (targetSize - drawHeight) / 2;

                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(cropped, new Rectangle(offsetX, offsetY, drawWidth, drawHeight));
            }

            var hIcon = resized.GetHicon();
            try
            {
                using var icon = Icon.FromHandle(hIcon);
                return (Icon)icon.Clone();
            }
            finally
            {
                NativeMethods.DestroyIcon(hIcon);
            }
        }
        catch
        {
            return GetDefaultTrayIcon();
        }
    }

    private static int GetTargetIconSize()
    {
        var size = SystemInformation.SmallIconSize;
        return Math.Max(size.Width, size.Height);
    }

    private static bool HasTransparentPixels(Bitmap bitmap)
    {
        var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var stride = data.Stride;
            var strideAbs = Math.Abs(stride);
            var bytes = strideAbs * data.Height;
            var buffer = new byte[bytes];
            Marshal.Copy(data.Scan0, buffer, 0, bytes);

            for (var y = 0; y < bitmap.Height; y++)
            {
                var rowStart = stride >= 0
                    ? y * strideAbs
                    : (bitmap.Height - 1 - y) * strideAbs;

                for (var x = 0; x < bitmap.Width; x++)
                {
                    if (buffer[rowStart + (x * 4) + 3] < 255)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static List<Color> SampleBackgroundColors(Bitmap bitmap)
    {
        var samples = new List<Color>();
        var step = Math.Max(1, Math.Min(bitmap.Width, bitmap.Height) / 32);

        for (var x = 0; x < bitmap.Width; x += step)
        {
            AddBackgroundSample(samples, bitmap.GetPixel(x, 0));
            AddBackgroundSample(samples, bitmap.GetPixel(x, bitmap.Height - 1));
        }

        for (var y = 0; y < bitmap.Height; y += step)
        {
            AddBackgroundSample(samples, bitmap.GetPixel(0, y));
            AddBackgroundSample(samples, bitmap.GetPixel(bitmap.Width - 1, y));
        }

        return samples;
    }

    private static void AddBackgroundSample(List<Color> samples, Color color)
    {
        const int tolerance = 6;
        foreach (var sample in samples)
        {
            if (ColorDistance(sample, color) <= tolerance)
            {
                return;
            }
        }
        samples.Add(color);
    }

    private static Bitmap ApplyBackgroundMask(Bitmap source, IReadOnlyList<Color> backgroundColors)
    {
        var bounds = new Rectangle(0, 0, source.Width, source.Height);
        var masked = source.Clone(bounds, PixelFormat.Format32bppArgb);
        var data = masked.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            var stride = data.Stride;
            var strideAbs = Math.Abs(stride);
            var bytes = strideAbs * data.Height;
            var buffer = new byte[bytes];
            Marshal.Copy(data.Scan0, buffer, 0, bytes);

            for (var y = 0; y < masked.Height; y++)
            {
                var rowStart = stride >= 0
                    ? y * strideAbs
                    : (masked.Height - 1 - y) * strideAbs;

                for (var x = 0; x < masked.Width; x++)
                {
                    var pixelIndex = rowStart + (x * 4);
                    var b = buffer[pixelIndex];
                    var g = buffer[pixelIndex + 1];
                    var r = buffer[pixelIndex + 2];
                    if (IsBackgroundColor(r, g, b, backgroundColors))
                    {
                        buffer[pixelIndex + 3] = 0;
                    }
                    else
                    {
                        buffer[pixelIndex + 3] = 255;
                    }
                }
            }

            Marshal.Copy(buffer, 0, data.Scan0, bytes);
        }
        finally
        {
            masked.UnlockBits(data);
        }

        return masked;
    }

    private static bool IsBackgroundColor(byte r, byte g, byte b, IReadOnlyList<Color> samples)
    {
        const int tolerance = 22;
        foreach (var sample in samples)
        {
            if (ColorDistance(sample, r, g, b) <= tolerance)
            {
                return true;
            }
        }
        return false;
    }

    private static int ColorDistance(Color left, Color right)
        => ColorDistance(left, right.R, right.G, right.B);

    private static int ColorDistance(Color left, byte r, byte g, byte b)
        => Math.Max(Math.Abs(left.R - r), Math.Max(Math.Abs(left.G - g), Math.Abs(left.B - b)));

    private static Rectangle GetLargestOpaqueBounds(Bitmap bitmap)
    {
        var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var stride = data.Stride;
            var strideAbs = Math.Abs(stride);
            var bytes = strideAbs * data.Height;
            var buffer = new byte[bytes];
            Marshal.Copy(data.Scan0, buffer, 0, bytes);

            var width = bitmap.Width;
            var height = bitmap.Height;
            var opaque = new bool[width * height];
            const byte alphaThreshold = 10;

            for (var y = 0; y < height; y++)
            {
                var rowStart = stride >= 0
                    ? y * strideAbs
                    : (height - 1 - y) * strideAbs;

                for (var x = 0; x < width; x++)
                {
                    var alpha = buffer[rowStart + (x * 4) + 3];
                    if (alpha > alphaThreshold)
                    {
                        opaque[(y * width) + x] = true;
                    }
                }
            }

            var visited = new bool[opaque.Length];
            var queue = new int[opaque.Length];
            var bestCount = 0;
            var bestBounds = Rectangle.Empty;

            for (var i = 0; i < opaque.Length; i++)
            {
                if (!opaque[i] || visited[i])
                {
                    continue;
                }

                var head = 0;
                var tail = 0;
                queue[tail++] = i;
                visited[i] = true;

                var count = 0;
                var minX = width;
                var maxX = -1;
                var minY = height;
                var maxY = -1;

                while (head < tail)
                {
                    var index = queue[head++];
                    count++;
                    var x = index % width;
                    var y = index / width;

                    if (x < minX)
                    {
                        minX = x;
                    }
                    if (x > maxX)
                    {
                        maxX = x;
                    }
                    if (y < minY)
                    {
                        minY = y;
                    }
                    if (y > maxY)
                    {
                        maxY = y;
                    }

                    if (x > 0)
                    {
                        TryVisit(index - 1, opaque, visited, queue, ref tail);
                    }
                    if (x + 1 < width)
                    {
                        TryVisit(index + 1, opaque, visited, queue, ref tail);
                    }
                    if (y > 0)
                    {
                        TryVisit(index - width, opaque, visited, queue, ref tail);
                    }
                    if (y + 1 < height)
                    {
                        TryVisit(index + width, opaque, visited, queue, ref tail);
                    }
                }

                if (count > bestCount && maxX >= minX && maxY >= minY)
                {
                    bestCount = count;
                    bestBounds = Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
                }
            }

            return bestBounds;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static void TryVisit(int index, bool[] opaque, bool[] visited, int[] queue, ref int tail)
    {
        if (opaque[index] && !visited[index])
        {
            visited[index] = true;
            queue[tail++] = index;
        }
    }

    private static Icon GetDefaultTrayIcon()
        => (Icon)SystemIcons.Application.Clone();
}
