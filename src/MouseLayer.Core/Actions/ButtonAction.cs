namespace MouseLayer.Core.Actions;

public enum ActionType
{
    /// <summary>No mapping configured — let the original button event pass through untouched.</summary>
    PassThrough,

    /// <summary>Swallow the button event; nothing is sent.</summary>
    Block,

    /// <summary>Simulate a different mouse button.</summary>
    MouseButton,

    /// <summary>Simulate a keyboard key.</summary>
    KeyPress,
}

/// <summary>What a source button (or wheel tick) should be turned into.</summary>
public sealed class ButtonAction
{
    public ActionType Type { get; init; } = ActionType.PassThrough;

    /// <summary>Used when Type is MouseButton.</summary>
    public MouseButton? TargetMouseButton { get; init; }

    /// <summary>Virtual-key code (Windows VK_*) used when Type is KeyPress.</summary>
    public ushort? VirtualKeyCode { get; init; }

    public static readonly ButtonAction PassThrough = new() { Type = ActionType.PassThrough };
    public static readonly ButtonAction Block = new() { Type = ActionType.Block };

    public static ButtonAction ToMouseButton(MouseButton target) =>
        new() { Type = ActionType.MouseButton, TargetMouseButton = target };

    public static ButtonAction ToKey(ushort virtualKeyCode) =>
        new() { Type = ActionType.KeyPress, VirtualKeyCode = virtualKeyCode };
}
