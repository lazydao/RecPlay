namespace RecPlay.Models;

internal sealed class UserSettings
{
    public string RecordHotkey { get; set; } = "Ctrl+Alt+R";
    public string PlaybackHotkey { get; set; } = "Ctrl+Alt+P";
    public int RepeatCount { get; set; } = 1;
    public int IntervalMs { get; set; } = 1000;
    public double PlaybackSpeed { get; set; } = 1;
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
}
