using System.Diagnostics;
using MouseLayer.Core.Native;

namespace MouseLayer.Core.Hooking;

public sealed class ForegroundAppChangedEventArgs : EventArgs
{
    public string ProcessName { get; }
    public ForegroundAppChangedEventArgs(string processName) => ProcessName = processName;
}

/// <summary>
/// Tracks which process owns the foreground window, so button mappings can switch per app.
/// Same message-loop requirement as <see cref="LowLevelMouseHook"/>.
/// </summary>
public sealed class ForegroundAppWatcher : IDisposable
{
    private readonly NativeMethods.WinEventProc _proc;
    private nint _hookHandle;

    public string CurrentProcessName { get; private set; } = string.Empty;

    public event EventHandler<ForegroundAppChangedEventArgs>? ForegroundChanged;

    public ForegroundAppWatcher()
    {
        _proc = OnWinEvent;
    }

    public void Install()
    {
        if (_hookHandle != 0)
            return;

        _hookHandle = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND,
            0, _proc, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);

        CurrentProcessName = ResolveForegroundProcessName();
    }

    private void OnWinEvent(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        var name = ResolveProcessName(hwnd);
        if (string.IsNullOrEmpty(name) || name == CurrentProcessName)
            return;

        CurrentProcessName = name;
        ForegroundChanged?.Invoke(this, new ForegroundAppChangedEventArgs(name));
    }

    private static string ResolveForegroundProcessName() => ResolveProcessName(NativeMethods.GetForegroundWindow());

    private static string ResolveProcessName(nint hwnd)
    {
        if (hwnd == 0)
            return string.Empty;

        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0)
            return string.Empty;

        try
        {
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName + ".exe";
        }
        catch (ArgumentException)
        {
            // Process exited between the event firing and us looking it up.
            return string.Empty;
        }
    }

    public void Dispose()
    {
        if (_hookHandle != 0)
        {
            NativeMethods.UnhookWinEvent(_hookHandle);
            _hookHandle = 0;
        }
    }
}
