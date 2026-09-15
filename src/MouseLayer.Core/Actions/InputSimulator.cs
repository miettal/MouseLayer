using MouseLayer.Core.Native;

namespace MouseLayer.Core.Actions;

/// <summary>Injects synthetic mouse/keyboard input via SendInput, to realize a remapped action.</summary>
public sealed class InputSimulator
{
    public void Press(ButtonAction action)
    {
        switch (action.Type)
        {
            case ActionType.MouseButton when action.TargetMouseButton is { } button:
                SendMouse(button, isDown: true);
                break;
            case ActionType.KeyPress when action.VirtualKeyCode is { } vk:
                SendKey(vk, isDown: true);
                break;
        }
    }

    public void Release(ButtonAction action)
    {
        switch (action.Type)
        {
            case ActionType.MouseButton when action.TargetMouseButton is { } button:
                SendMouse(button, isDown: false);
                break;
            case ActionType.KeyPress when action.VirtualKeyCode is { } vk:
                SendKey(vk, isDown: false);
                break;
        }
    }

    /// <summary>Fires a full press+release, for one-shot triggers like wheel ticks.</summary>
    public void Click(ButtonAction action)
    {
        Press(action);
        Release(action);
    }

    private static void SendMouse(MouseButton button, bool isDown)
    {
        var flags = button switch
        {
            MouseButton.Left => isDown ? NativeMethods.MOUSEEVENTF_LEFTDOWN : NativeMethods.MOUSEEVENTF_LEFTUP,
            MouseButton.Right => isDown ? NativeMethods.MOUSEEVENTF_RIGHTDOWN : NativeMethods.MOUSEEVENTF_RIGHTUP,
            MouseButton.Middle => isDown ? NativeMethods.MOUSEEVENTF_MIDDLEDOWN : NativeMethods.MOUSEEVENTF_MIDDLEUP,
            MouseButton.X1 or MouseButton.X2 => isDown ? NativeMethods.MOUSEEVENTF_XDOWN : NativeMethods.MOUSEEVENTF_XUP,
            _ => 0u,
        };

        if (flags == 0)
            return;

        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_MOUSE,
            u = new NativeMethods.INPUTUNION
            {
                mi = new NativeMethods.MOUSEINPUT
                {
                    dwFlags = flags,
                    mouseData = button == MouseButton.X1 ? 1u : button == MouseButton.X2 ? 2u : 0u,
                },
            },
        };

        NativeMethods.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void SendKey(ushort virtualKeyCode, bool isDown)
    {
        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = virtualKeyCode,
                    dwFlags = isDown ? 0u : NativeMethods.KEYEVENTF_KEYUP,
                },
            },
        };

        NativeMethods.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
    }
}
