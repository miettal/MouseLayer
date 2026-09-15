using MouseLayer.Core;
using MouseLayer.Core.Actions;
using MouseLayer.Core.Profiles;

namespace MouseLayer.Core.IntegrationTests;

/// <summary>
/// End-to-end checks that the real WH_MOUSE_LL hook + SendInput pipeline works on this
/// machine. These need an interactive Windows desktop session (which GitHub-hosted
/// windows-latest runners provide) — they will not run meaningfully on Linux/macOS
/// or in a locked/headless Windows session.
/// </summary>
public class MouseRemapIntegrationTests
{
    // An unusual virtual-key unlikely to be sent by anything else running in CI.
    private const ushort TargetVirtualKey = 0x88; // VK 0x88 is unassigned/reserved.

    [Fact]
    public void LeftClick_MappedToKeyPress_ReachesSystemAsThatKey()
    {
        var config = new MouseLayerConfig
        {
            Profiles =
            {
                new AppProfile
                {
                    ProcessNamePattern = "*",
                    Mappings = { new ButtonMapping { Source = MouseButton.Left, Action = ButtonAction.ToKey(TargetVirtualKey) } },
                },
            },
        };

        var harness = new StaHookTestHarness(config, TargetVirtualKey, () =>
        {
            new InputSimulator().Click(ButtonAction.ToMouseButton(MouseButton.Left));
        });

        var (keyDownObserved, keyUpObserved) = harness.Run(TimeSpan.FromSeconds(15));

        Assert.True(keyDownObserved, "Expected the remapped key-down to reach the system input queue.");
        Assert.True(keyUpObserved, "Expected the remapped key-up to reach the system input queue.");
    }
}
