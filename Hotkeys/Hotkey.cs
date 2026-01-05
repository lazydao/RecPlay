using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RecPlay.Hotkeys;

[Flags]
internal enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8,
}

internal readonly struct Hotkey
{
    public Hotkey(HotkeyModifiers modifiers, Keys key)
    {
        Modifiers = modifiers;
        Key = key;
    }

    public HotkeyModifiers Modifiers { get; }
    public Keys Key { get; }

    public bool IsEmpty => Key == Keys.None;

    public override string ToString()
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }
        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }
        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }
        if (Modifiers.HasFlag(HotkeyModifiers.Win))
        {
            parts.Add("Win");
        }
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }

    public static bool TryParse(string text, out Hotkey hotkey)
    {
        hotkey = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var tokens = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return false;
        }

        var modifiers = HotkeyModifiers.None;
        Keys key = Keys.None;

        foreach (var token in tokens)
        {
            var normalized = token.Trim();
            if (normalized.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.Control;
                continue;
            }
            if (normalized.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.Alt;
                continue;
            }
            if (normalized.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.Shift;
                continue;
            }
            if (normalized.Equals("Win", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.Win;
                continue;
            }

            if (normalized.Length == 1 && char.IsDigit(normalized[0]))
            {
                var digit = normalized[0] - '0';
                key = Keys.D0 + digit;
                continue;
            }

            if (Enum.TryParse<Keys>(normalized, true, out var parsed))
            {
                key = parsed;
                continue;
            }
        }

        if (key == Keys.None)
        {
            return false;
        }

        hotkey = new Hotkey(modifiers, key);
        return true;
    }

    public static bool operator ==(Hotkey left, Hotkey right)
        => left.Modifiers == right.Modifiers && left.Key == right.Key;

    public static bool operator !=(Hotkey left, Hotkey right) => !(left == right);

    public override bool Equals(object? obj)
        => obj is Hotkey other && this == other;

    public override int GetHashCode()
        => HashCode.Combine((int)Modifiers, (int)Key);
}
