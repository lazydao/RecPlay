using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RecPlay.Models;
using RecPlay.Native;

namespace RecPlay.Services;

internal sealed class Recorder : IDisposable
{
    private readonly List<InputEvent> _events = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly object _lock = new();
    private IntPtr _mouseHook = IntPtr.Zero;
    private NativeMethods.LowLevelMouseProc? _mouseProc;
    private int _lastMouseX;
    private int _lastMouseY;
    private bool _hasLastMouse;

    public bool IsRecording { get; private set; }

    public bool Start()
    {
        if (IsRecording)
        {
            return true;
        }

        _events.Clear();
        _stopwatch.Restart();
        _hasLastMouse = false;

        _mouseProc = MouseHookCallback;
        var moduleHandle = NativeMethods.GetCurrentModuleHandle();
        _mouseHook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL,
            _mouseProc,
            moduleHandle,
            0);

        if (_mouseHook == IntPtr.Zero)
        {
            CleanupHooks();
            _stopwatch.Reset();
            return false;
        }

        IsRecording = true;
        return true;
    }

    public IReadOnlyList<InputEvent> Stop()
    {
        IsRecording = false;
        _stopwatch.Stop();
        CleanupHooks();

        return new List<InputEvent>(_events);
    }

    public void Dispose()
    {
        Stop();
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && IsRecording)
        {
            var message = wParam.ToInt32();
            var data = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

            switch (message)
            {
                case NativeMethods.WM_MOUSEMOVE:
                    if (!_hasLastMouse || data.pt.x != _lastMouseX || data.pt.y != _lastMouseY)
                    {
                        _lastMouseX = data.pt.x;
                        _lastMouseY = data.pt.y;
                        _hasLastMouse = true;
                        AddEvent(new InputEvent
                        {
                            EventType = "MouseMove",
                            TimeOffsetMs = _stopwatch.ElapsedMilliseconds,
                            X = data.pt.x,
                            Y = data.pt.y,
                        });
                    }
                    break;
                case NativeMethods.WM_LBUTTONDOWN:
                case NativeMethods.WM_LBUTTONUP:
                    AddMouseButtonEvent(0, message == NativeMethods.WM_LBUTTONDOWN, data.pt.x, data.pt.y);
                    break;
                case NativeMethods.WM_RBUTTONDOWN:
                case NativeMethods.WM_RBUTTONUP:
                    AddMouseButtonEvent(1, message == NativeMethods.WM_RBUTTONDOWN, data.pt.x, data.pt.y);
                    break;
                case NativeMethods.WM_MBUTTONDOWN:
                case NativeMethods.WM_MBUTTONUP:
                    AddMouseButtonEvent(2, message == NativeMethods.WM_MBUTTONDOWN, data.pt.x, data.pt.y);
                    break;
                case NativeMethods.WM_XBUTTONDOWN:
                case NativeMethods.WM_XBUTTONUP:
                    var xButton = HIWORD(data.mouseData) == NativeMethods.XBUTTON1 ? 3 : 4;
                    AddMouseButtonEvent(xButton, message == NativeMethods.WM_XBUTTONDOWN, data.pt.x, data.pt.y);
                    break;
                case NativeMethods.WM_MOUSEWHEEL:
                    var delta = (short)HIWORD(data.mouseData);
                    AddEvent(new InputEvent
                    {
                        EventType = "MouseWheel",
                        TimeOffsetMs = _stopwatch.ElapsedMilliseconds,
                        X = data.pt.x,
                        Y = data.pt.y,
                        WheelDelta = delta,
                    });
                    break;
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private void AddMouseButtonEvent(int button, bool isDown, int x, int y)
    {
        AddEvent(new InputEvent
        {
            EventType = "MouseButton",
            TimeOffsetMs = _stopwatch.ElapsedMilliseconds,
            X = x,
            Y = y,
            MouseButton = button,
            ButtonDown = isDown,
        });
    }

    private void AddEvent(InputEvent inputEvent)
    {
        lock (_lock)
        {
            _events.Add(inputEvent);
        }
    }

    private void CleanupHooks()
    {
        if (_mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }

    private static int HIWORD(uint number)
        => (short)((number >> 16) & 0xffff);
}
