using MouseLayer.Core.Native;

namespace MouseLayer.Core.Hooking;

public sealed class MouseButtonEventArgs : EventArgs
{
    public MouseButton Button { get; }

    /// <summary>Set true to stop the event from reaching the OS / other apps.</summary>
    public bool Handled { get; set; }

    public MouseButtonEventArgs(MouseButton button) => Button = button;
}

/// <summary>
/// Wraps a WH_MOUSE_LL hook. Must be created on a thread that runs a Win32 message loop
/// (e.g. the WinForms UI thread via Application.Run) — the hook callback is pumped through
/// that thread's message queue, so it silently stops firing on a thread with no loop.
/// </summary>
public sealed class LowLevelMouseHook : IDisposable
{
    private readonly NativeMethods.LowLevelMouseProc _proc;
    private nint _hookHandle;

    public event EventHandler<MouseButtonEventArgs>? ButtonDown;
    public event EventHandler<MouseButtonEventArgs>? ButtonUp;

    public LowLevelMouseHook()
    {
        _proc = HookCallback;
    }

    public void Install()
    {
        if (_hookHandle != 0)
            return;

        using var curModule = System.Diagnostics.Process.GetCurrentProcess().MainModule
            ?? throw new InvalidOperationException("Could not resolve main module for hook installation.");
        var moduleHandle = NativeMethods.GetModuleHandle(curModule.ModuleName);

        _hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _proc, moduleHandle, 0);
        if (_hookHandle == 0)
            throw new InvalidOperationException("Failed to install low-level mouse hook.");
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var msg = (int)wParam;
            var (button, isDown) = Classify(msg, lParam);

            if (button is { } b)
            {
                var args = new MouseButtonEventArgs(b);
                if (isDown)
                    ButtonDown?.Invoke(this, args);
                else
                    ButtonUp?.Invoke(this, args);

                if (args.Handled)
                    return 1;
            }
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static (MouseButton? button, bool isDown) Classify(int msg, nint lParam)
    {
        switch (msg)
        {
            case NativeMethods.WM_LBUTTONDOWN: return (MouseButton.Left, true);
            case NativeMethods.WM_LBUTTONUP: return (MouseButton.Left, false);
            case NativeMethods.WM_RBUTTONDOWN: return (MouseButton.Right, true);
            case NativeMethods.WM_RBUTTONUP: return (MouseButton.Right, false);
            case NativeMethods.WM_MBUTTONDOWN: return (MouseButton.Middle, true);
            case NativeMethods.WM_MBUTTONUP: return (MouseButton.Middle, false);
            case NativeMethods.WM_XBUTTONDOWN:
            case NativeMethods.WM_XBUTTONUP:
            {
                var hookStruct = System.Runtime.InteropServices.Marshal
                    .PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                var xButton = (int)(hookStruct.mouseData >> 16);
                var button = xButton == NativeMethods.XBUTTON1 ? MouseButton.X1 : MouseButton.X2;
                return (button, msg == NativeMethods.WM_XBUTTONDOWN);
            }
            case NativeMethods.WM_MOUSEWHEEL:
            {
                var hookStruct = System.Runtime.InteropServices.Marshal
                    .PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                var delta = (short)(hookStruct.mouseData >> 16);
                // Wheel ticks have no natural "up" transition, so they are only ever reported as ButtonDown.
                return (delta > 0 ? MouseButton.WheelUp : MouseButton.WheelDown, true);
            }
            default:
                return (null, false);
        }
    }

    public void Dispose()
    {
        if (_hookHandle != 0)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }
    }
}
