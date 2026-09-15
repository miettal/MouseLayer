using System.Runtime.InteropServices;

namespace MouseLayer.Core.IntegrationTests;

/// <summary>
/// A standalone WH_KEYBOARD_LL hook used only by tests, to observe whether a key
/// MouseLayerEngine injects via SendInput actually reaches the system input queue.
/// Kept separate from MouseLayer.Core's internals so tests exercise the real,
/// externally-observable effect rather than internal state.
/// </summary>
internal sealed class KeyboardVerificationHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public nint dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandle(string? lpModuleName);

    private readonly LowLevelKeyboardProc _proc;
    private readonly ushort _watchedVirtualKey;
    private nint _hookHandle;

    public bool KeyDownObserved { get; private set; }
    public bool KeyUpObserved { get; private set; }

    public KeyboardVerificationHook(ushort watchedVirtualKey)
    {
        _watchedVirtualKey = watchedVirtualKey;
        _proc = HookCallback;

        using var curModule = System.Diagnostics.Process.GetCurrentProcess().MainModule!;
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
        if (_hookHandle == 0)
            throw new InvalidOperationException("Failed to install verification keyboard hook.");
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var info = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if (info.vkCode == _watchedVirtualKey)
            {
                if ((int)wParam == WM_KEYDOWN) KeyDownObserved = true;
                if ((int)wParam == WM_KEYUP) KeyUpObserved = true;
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookHandle != 0)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }
    }
}
