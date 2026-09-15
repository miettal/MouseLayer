using MouseLayer.Core.Actions;
using MouseLayer.Core.Hooking;
using MouseLayer.Core.Profiles;

namespace MouseLayer.Core;

/// <summary>
/// Wires the mouse hook, foreground-app watcher and configured profiles together.
/// Must be started on the UI thread (see <see cref="LowLevelMouseHook"/>).
/// </summary>
public sealed class MouseLayerEngine : IDisposable
{
    private readonly LowLevelMouseHook _hook = new();
    private readonly ForegroundAppWatcher _foregroundWatcher = new();
    private readonly InputSimulator _inputSimulator = new();

    public MouseLayerConfig Config { get; set; }

    public MouseLayerEngine(MouseLayerConfig config)
    {
        Config = config;
        _hook.ButtonDown += OnButtonDown;
        _hook.ButtonUp += OnButtonUp;
    }

    public void Start()
    {
        _foregroundWatcher.Install();
        _hook.Install();
    }

    private static bool IsWheel(MouseButton button) =>
        button is MouseButton.WheelUp or MouseButton.WheelDown;

    private void OnButtonDown(object? sender, MouseButtonEventArgs e)
    {
        var action = CurrentAction(e.Button);
        if (action.Type == ActionType.PassThrough)
            return;

        e.Handled = true;

        if (action.Type == ActionType.Block)
            return;

        // Wheel ticks have no matching "up" event, so fire the full press+release here.
        if (IsWheel(e.Button))
            _inputSimulator.Click(action);
        else
            _inputSimulator.Press(action);
    }

    private void OnButtonUp(object? sender, MouseButtonEventArgs e)
    {
        var action = CurrentAction(e.Button);
        if (action.Type == ActionType.PassThrough)
            return;

        e.Handled = true;

        if (action.Type is ActionType.MouseButton or ActionType.KeyPress)
            _inputSimulator.Release(action);
    }

    private ButtonAction CurrentAction(MouseButton button) =>
        Config.FindProfile(_foregroundWatcher.CurrentProcessName)?.GetAction(button) ?? ButtonAction.PassThrough;

    public void Dispose()
    {
        _hook.Dispose();
        _foregroundWatcher.Dispose();
    }
}
