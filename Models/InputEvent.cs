namespace RecPlay.Models;

internal sealed class InputEvent
{
    public string EventType { get; set; } = string.Empty;
    public long TimeOffsetMs { get; set; }

    public int KeyCode { get; set; }
    public bool KeyDown { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    public int MouseButton { get; set; }
    public bool ButtonDown { get; set; }
    public int WheelDelta { get; set; }
}
