using System;
using System.Windows.Forms;
using RecPlay.Native;

namespace RecPlay.Hotkeys;

internal sealed class HotkeyBox : TextBox
{
    private Hotkey _hotkey;

    public HotkeyBox()
    {
        ReadOnly = true;
        TabStop = true;
    }

    public Hotkey Hotkey
    {
        get => _hotkey;
        set
        {
            _hotkey = value;
            Text = _hotkey.ToString();
        }
    }

    public event EventHandler? HotkeyChanged;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        e.SuppressKeyPress = true;

        var key = e.KeyCode;
        if (IsModifierKey(key))
        {
            return;
        }

        var modifiers = HotkeyModifiers.None;
        if (e.Control)
        {
            modifiers |= HotkeyModifiers.Control;
        }
        if (e.Alt)
        {
            modifiers |= HotkeyModifiers.Alt;
        }
        if (e.Shift)
        {
            modifiers |= HotkeyModifiers.Shift;
        }
        if (IsWinPressed())
        {
            modifiers |= HotkeyModifiers.Win;
        }

        Hotkey = new Hotkey(modifiers, key);
        HotkeyChanged?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsModifierKey(Keys key)
        => key == Keys.ControlKey ||
           key == Keys.LControlKey ||
           key == Keys.RControlKey ||
           key == Keys.ShiftKey ||
           key == Keys.LShiftKey ||
           key == Keys.RShiftKey ||
           key == Keys.Menu ||
           key == Keys.LMenu ||
           key == Keys.RMenu ||
           key == Keys.LWin ||
           key == Keys.RWin;

    private static bool IsWinPressed()
        => (NativeMethods.GetKeyState((int)Keys.LWin) & 0x8000) != 0 ||
           (NativeMethods.GetKeyState((int)Keys.RWin) & 0x8000) != 0;
}
