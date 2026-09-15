using MouseLayer.Core.Actions;

namespace MouseLayer.Core.Profiles;

public sealed class ButtonMapping
{
    public MouseButton Source { get; init; }
    public ButtonAction Action { get; init; } = ButtonAction.PassThrough;
}
