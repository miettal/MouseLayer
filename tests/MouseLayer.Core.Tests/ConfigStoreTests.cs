using MouseLayer.Core;
using MouseLayer.Core.Actions;
using MouseLayer.Core.Profiles;

namespace MouseLayer.Core.Tests;

public class ConfigStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"mouselayer-test-{Guid.NewGuid():N}.json");

    [Fact]
    public void Load_CreatesDefaultConfigFile_WhenNoneExists()
    {
        Assert.False(File.Exists(_path));

        var config = ConfigStore.Load(_path);

        Assert.True(File.Exists(_path));
        var profile = Assert.Single(config.Profiles);
        Assert.True(profile.IsDefault);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsProfilesAndMappings()
    {
        var original = new MouseLayerConfig
        {
            Profiles =
            {
                new AppProfile
                {
                    ProcessNamePattern = "chrome.exe",
                    Mappings =
                    {
                        new ButtonMapping { Source = MouseButton.X1, Action = ButtonAction.ToMouseButton(MouseButton.Left) },
                        new ButtonMapping { Source = MouseButton.WheelUp, Action = ButtonAction.ToKey(0x26) },
                        new ButtonMapping { Source = MouseButton.Middle, Action = ButtonAction.Block },
                    },
                },
            },
        };

        ConfigStore.Save(original, _path);
        var loaded = ConfigStore.Load(_path);

        var profile = Assert.Single(loaded.Profiles);
        Assert.Equal("chrome.exe", profile.ProcessNamePattern);

        var x1Action = profile.GetAction(MouseButton.X1);
        Assert.Equal(ActionType.MouseButton, x1Action.Type);
        Assert.Equal(MouseButton.Left, x1Action.TargetMouseButton);

        var wheelAction = profile.GetAction(MouseButton.WheelUp);
        Assert.Equal(ActionType.KeyPress, wheelAction.Type);
        Assert.Equal((ushort)0x26, wheelAction.VirtualKeyCode);

        var middleAction = profile.GetAction(MouseButton.Middle);
        Assert.Equal(ActionType.Block, middleAction.Type);
    }

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }
}
