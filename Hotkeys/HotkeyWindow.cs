using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RecPlay.Native;

namespace RecPlay.Hotkeys;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private readonly HashSet<int> _registeredIds = new();

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    public event EventHandler<int>? HotkeyPressed;

    public bool RegisterHotkey(int id, Hotkey hotkey)
    {
        if (hotkey.IsEmpty)
        {
            return false;
        }

        var modifiers = ToNativeModifiers(hotkey.Modifiers);
        var key = (uint)hotkey.Key;
        var success = NativeMethods.RegisterHotKey(Handle, id, modifiers, key);
        if (success)
        {
            _registeredIds.Add(id);
        }
        return success;
    }

    public void UnregisterHotkey(int id)
    {
        if (_registeredIds.Remove(id))
        {
            NativeMethods.UnregisterHotKey(Handle, id);
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _registeredIds)
        {
            NativeMethods.UnregisterHotKey(Handle, id);
        }
        _registeredIds.Clear();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey)
        {
            HotkeyPressed?.Invoke(this, m.WParam.ToInt32());
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterAll();
        DestroyHandle();
    }

    private static uint ToNativeModifiers(HotkeyModifiers modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            result |= NativeMethods.MOD_ALT;
        }
        if (modifiers.HasFlag(HotkeyModifiers.Control))
        {
            result |= NativeMethods.MOD_CONTROL;
        }
        if (modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            result |= NativeMethods.MOD_SHIFT;
        }
        if (modifiers.HasFlag(HotkeyModifiers.Win))
        {
            result |= NativeMethods.MOD_WIN;
        }
        return result;
    }
}
