using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using RecPlay.Models;
using RecPlay.Native;

namespace RecPlay.Services;

internal sealed class Player
{
    public async Task PlayAsync(IReadOnlyList<InputEvent> events, int repeatCount, int intervalMs, double playbackSpeed, CancellationToken token)
    {
        if (events.Count == 0 || repeatCount <= 0)
        {
            return;
        }

        var repeats = Math.Max(1, repeatCount);
        var interval = Math.Max(0, intervalMs);
        var speed = Math.Clamp(playbackSpeed, 0.5, 5.0);

        for (var i = 0; i < repeats; i++)
        {
            long previousOffset = 0;
            foreach (var inputEvent in events)
            {
                token.ThrowIfCancellationRequested();

                var delay = inputEvent.TimeOffsetMs - previousOffset;
                var adjustedDelay = delay / speed;
                if (adjustedDelay > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(adjustedDelay), token).ConfigureAwait(false);
                }

                DispatchEvent(inputEvent);
                previousOffset = inputEvent.TimeOffsetMs;
            }

            if (i < repeats - 1 && interval > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(interval), token).ConfigureAwait(false);
            }
        }
    }

    private static void DispatchEvent(InputEvent inputEvent)
    {
        switch (inputEvent.EventType)
        {
            case "MouseMove":
                NativeMethods.SetCursorPos(inputEvent.X, inputEvent.Y);
                break;
            case "MouseButton":
                SendMouseButton(inputEvent);
                break;
            case "MouseWheel":
                SendMouseWheel(inputEvent);
                break;
        }
    }

    private static void SendMouseButton(InputEvent inputEvent)
    {
        NativeMethods.SetCursorPos(inputEvent.X, inputEvent.Y);

        uint flags = inputEvent.MouseButton switch
        {
            0 => inputEvent.ButtonDown ? NativeMethods.MOUSEEVENTF_LEFTDOWN : NativeMethods.MOUSEEVENTF_LEFTUP,
            1 => inputEvent.ButtonDown ? NativeMethods.MOUSEEVENTF_RIGHTDOWN : NativeMethods.MOUSEEVENTF_RIGHTUP,
            2 => inputEvent.ButtonDown ? NativeMethods.MOUSEEVENTF_MIDDLEDOWN : NativeMethods.MOUSEEVENTF_MIDDLEUP,
            3 or 4 => inputEvent.ButtonDown ? NativeMethods.MOUSEEVENTF_XDOWN : NativeMethods.MOUSEEVENTF_XUP,
            _ => 0,
        };

        uint mouseData = 0;
        if (inputEvent.MouseButton == 3)
        {
            mouseData = NativeMethods.XBUTTON1;
        }
        else if (inputEvent.MouseButton == 4)
        {
            mouseData = NativeMethods.XBUTTON2;
        }

        if (flags == 0)
        {
            return;
        }

        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_MOUSE,
            U = new NativeMethods.InputUnion
            {
                mi = new NativeMethods.MOUSEINPUT
                {
                    dwFlags = flags,
                    mouseData = mouseData,
                },
            },
        };

        var inputs = new[] { input };
        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void SendMouseWheel(InputEvent inputEvent)
    {
        NativeMethods.SetCursorPos(inputEvent.X, inputEvent.Y);

        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_MOUSE,
            U = new NativeMethods.InputUnion
            {
                mi = new NativeMethods.MOUSEINPUT
                {
                    dwFlags = NativeMethods.MOUSEEVENTF_WHEEL,
                    mouseData = unchecked((uint)inputEvent.WheelDelta),
                },
            },
        };

        var inputs = new[] { input };
        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }
}
