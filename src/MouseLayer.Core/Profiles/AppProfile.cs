using MouseLayer.Core.Actions;

namespace MouseLayer.Core.Profiles;

public sealed class AppProfile
{
    /// <summary>Executable name this profile applies to (e.g. "chrome.exe"), or "*" for the default/fallback profile.</summary>
    public string ProcessNamePattern { get; init; } = "*";

    public List<ButtonMapping> Mappings { get; init; } = new();

    public bool IsDefault => ProcessNamePattern == "*";

    public bool Matches(string processName) =>
        IsDefault || string.Equals(ProcessNamePattern, processName, StringComparison.OrdinalIgnoreCase);

    public ButtonAction GetAction(MouseButton source) =>
        Mappings.FirstOrDefault(m => m.Source == source)?.Action ?? ButtonAction.PassThrough;
}
